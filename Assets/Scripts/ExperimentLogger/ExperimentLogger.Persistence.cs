using System;
using System.Collections;
using System.IO;
using System.Globalization;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

public partial class ExperimentLogger
{
    public void EndAndSaveSession()
    {
        if (session == null || saveStarted) return;
        // Close any open scene visit before finalizing session
        if (hasOpenSceneVisit)
        {
            EndSceneVisit(null);
        }
        session.endTime = DateTime.UtcNow.ToString("o");
        saveStarted = true;
        StartCoroutine(SendSessionToSupabase());
    }

    // Public coroutine to explicitly flush the session now and wait until it is sent
    public IEnumerator FlushAndSaveSession()
    {
        if (session == null)
            yield break;

        if (!saveStarted)
        {
            session.endTime = DateTime.UtcNow.ToString("o");
            saveStarted = true;
            yield return SendSessionToSupabase();
        }
        else
        {
            // Already started elsewhere (e.g., OnApplicationQuit). Nothing to wait on here
            // because we don't keep a handle to the running coroutine. Best effort: just exit.
            yield break;
        }
    }

    private IEnumerator SendSessionToSupabase()
    {
        // Treat placeholder values as not configured to avoid noisy web errors
        bool notConfigured = string.IsNullOrWhiteSpace(supabaseUrl)
                             || string.IsNullOrWhiteSpace(supabaseAnonKey)
                             || string.IsNullOrWhiteSpace(supabaseTable)
                             || supabaseUrl.Contains("YOUR-PROJECT-REF")
                             || supabaseAnonKey.Contains("YOUR-ANON");

        // Always keep the full log on disk first, synchronously: survives offline play and app quit,
        // and is what the clinician report reads.
        SaveSessionLocalNow();

        if (notConfigured)
        {
            Debug.LogWarning("[ExperimentLogger] Supabase not configured. Session kept locally only.");
            yield break;
        }

        // Per request: send only the summary line in the JSON log to Supabase.
        string sessionExportJson = BuildSummaryOnlyJson();
        string jsonBody = BuildSupabaseRowJson(sessionExportJson);

        string url = $"{supabaseUrl}/rest/v1/{supabaseTable}";

        using (UnityWebRequest req = new UnityWebRequest(url, "POST"))
        {
            byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonBody);
            req.uploadHandler = new UploadHandlerRaw(bodyRaw);
            req.downloadHandler = new DownloadHandlerBuffer();

            req.SetRequestHeader("Content-Type", "application/json");
            req.SetRequestHeader("apikey", supabaseAnonKey);
            req.SetRequestHeader("Authorization", "Bearer " + supabaseAnonKey);
            req.SetRequestHeader("Prefer", "return=minimal");

            yield return req.SendWebRequest();

            if (req.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError($"[ExperimentLogger] Supabase error: {req.responseCode} {req.error}\n{req.downloadHandler.text}");
            }
            else
            {
                Debug.Log("[ExperimentLogger] Session successfully saved to Supabase.");
            }
        }

        // After sending the summary line to Supabase, send the features to the ML model API (if enabled)
        if (enableModelApiCall && !string.IsNullOrWhiteSpace(modelApiUrl))
        {
            double[] features = BuildModelFeatureVector();
            if (features != null && features.Length > 0)
            {
                yield return SendFeaturesToModel(features);
            }
        }
    }

    // Build a minimal JSON that contains only the summary line.
    // This is used exclusively for the Supabase upload so that session_json
    // holds just the summary while the full log remains saved locally.
    private string BuildSummaryOnlyJson()
    {
        // Compute the same summary string as used elsewhere
        int _screwsSpawned = 0, _screwsCollected = 0;
        int _forestFishCollisions = 0, _forestApproaches = 0, _forestOutOfBounds = 0, _forestCollected5 = 0;
        float _targetRtMedian = 0f, _targetRtCv = 0f;
        int _forestKeyCount = 0; float _forestKeyGapMean = 0f, _forestKeyGapCv = 0f;
        int _riverFishCollisions = 0, _riverApproaches = 0, _riverOutOfBounds = 0, _riverWins = 0;
        int _riverKeyCount = 0; float _riverKeyGapMean = 0f, _riverKeyGapCv = 0f;
        int _dwarfEnters = 0; float _dwarfWmRtMedian = 0f, _dwarfWmRtCv = 0f;
        int _dwarfCorrect = 0, _dwarfIncorrect = 0;
        try { ComputeScrewCounts(out _screwsSpawned, out _screwsCollected); } catch { }
        try { ComputeForestFishCollisionAndApproachCounts(out _forestFishCollisions, out _forestApproaches); } catch { }
        try { ComputeForestOutOfBoundsCount(out _forestOutOfBounds); } catch { }
        try { ComputeForestCollected5ScrewsCount(out _forestCollected5); } catch { }
        try { ComputeScrewTargetRTMetrics(out _targetRtMedian, out _targetRtCv); } catch { }
        try { ComputeForestKeyPressMetrics(out _forestKeyCount, out _forestKeyGapMean, out _forestKeyGapCv); } catch { }
        try { ComputeRiverFishCollisionAndApproachCounts(out _riverFishCollisions, out _riverApproaches); } catch { }
        try { ComputeRiverOutOfBoundsCount(out _riverOutOfBounds); } catch { }
        try { ComputeRiverWinCount(out _riverWins); } catch { }
        try { ComputeRiverKeyPressMetrics(out _riverKeyCount, out _riverKeyGapMean, out _riverKeyGapCv); } catch { }
        try { ComputeDwarfEnterCount(out _dwarfEnters); } catch { }
        try { ComputeDwarfWmRtMetrics(out _dwarfWmRtMedian, out _dwarfWmRtCv); } catch { }
        try { ComputeDwarfCorrectIncorrectCounts(out _dwarfCorrect, out _dwarfIncorrect); } catch { }

        string _summaryLine =
            $"screws_spawned={_screwsSpawned}, screws_collected={_screwsCollected}, " +
            $"forest_fish_collisions={_forestFishCollisions}, forest_out_of_bounds={_forestOutOfBounds}, forest_5_screws_collected={_forestCollected5}, forest_approaches={_forestApproaches}, " +
            $"target_rt_median={R2(_targetRtMedian)}, target_rt_cv={R2(_targetRtCv)}, " +
            $"forest_keys={_forestKeyCount}, forest_key_gap_mean={R2(_forestKeyGapMean)}, forest_key_gap_cv={R2(_forestKeyGapCv)}, " +
            $"river_fish_collisions={_riverFishCollisions}, river_out_of_bounds={_riverOutOfBounds}, river_win={_riverWins}, river_approaches={_riverApproaches}, " +
            $"river_keys={_riverKeyCount}, river_key_gap_mean={R2(_riverKeyGapMean)}, river_key_gap_cv={R2(_riverKeyGapCv)}, " +
            $"dwarf_enters={_dwarfEnters}, dwarf_wm_rt_median={R2(_dwarfWmRtMedian)}, dwarf_wm_rt_cv={R2(_dwarfWmRtCv)}, " +
            $"dwarf_correct={_dwarfCorrect}, dwarf_incorrect={_dwarfIncorrect}";

        // Return minimal JSON object with only the summary line
        var sb = new StringBuilder();
        sb.Append('{');
        sb.Append("\"summaryLine\":\"").Append(EscapeJsonString(_summaryLine)).Append('\"');
        sb.Append('}');
        return sb.ToString();
    }

    // Build the numeric feature vector in the exact same order as the summary line.
    // Order (23):
    // 0 screws_spawned, 1 screws_collected,
    // 2 forest_fish_collisions, 3 forest_out_of_bounds, 4 forest_5_screws_collected, 5 forest_approaches,
    // 6 target_rt_median, 7 target_rt_cv,
    // 8 forest_keys, 9 forest_key_gap_mean, 10 forest_key_gap_cv,
    // 11 river_fish_collisions, 12 river_out_of_bounds, 13 river_win, 14 river_approaches,
    // 15 river_keys, 16 river_key_gap_mean, 17 river_key_gap_cv,
    // 18 dwarf_enters, 19 dwarf_wm_rt_median, 20 dwarf_wm_rt_cv,
    // 21 dwarf_correct, 22 dwarf_incorrect
    private double[] BuildModelFeatureVector()
    {
        int screwsSpawned = 0, screwsCollected = 0;
        int forestFishCollisions = 0, forestApproaches = 0, forestOutOfBounds = 0, forestCollected5 = 0;
        float targetRtMedian = 0f, targetRtCv = 0f;
        int forestKeyCount = 0; float forestKeyGapMean = 0f, forestKeyGapCv = 0f;
        int riverFishCollisions = 0, riverApproaches = 0, riverOutOfBounds = 0, riverWins = 0;
        int riverKeyCount = 0; float riverKeyGapMean = 0f, riverKeyGapCv = 0f;
        int dwarfEnters = 0; float dwarfWmRtMedian = 0f, dwarfWmRtCv = 0f; int dwarfCorrect = 0, dwarfIncorrect = 0;

        try { ComputeScrewCounts(out screwsSpawned, out screwsCollected); } catch { }
        try { ComputeForestFishCollisionAndApproachCounts(out forestFishCollisions, out forestApproaches); } catch { }
        try { ComputeForestOutOfBoundsCount(out forestOutOfBounds); } catch { }
        try { ComputeForestCollected5ScrewsCount(out forestCollected5); } catch { }
        try { ComputeScrewTargetRTMetrics(out targetRtMedian, out targetRtCv); } catch { }
        try { ComputeForestKeyPressMetrics(out forestKeyCount, out forestKeyGapMean, out forestKeyGapCv); } catch { }
        try { ComputeRiverFishCollisionAndApproachCounts(out riverFishCollisions, out riverApproaches); } catch { }
        try { ComputeRiverOutOfBoundsCount(out riverOutOfBounds); } catch { }
        try { ComputeRiverWinCount(out riverWins); } catch { }
        try { ComputeRiverKeyPressMetrics(out riverKeyCount, out riverKeyGapMean, out riverKeyGapCv); } catch { }
        try { ComputeDwarfEnterCount(out dwarfEnters); } catch { }
        try { ComputeDwarfWmRtMetrics(out dwarfWmRtMedian, out dwarfWmRtCv); } catch { }
        try { ComputeDwarfCorrectIncorrectCounts(out dwarfCorrect, out dwarfIncorrect); } catch { }

        var feats = new double[23];
        feats[0] = screwsSpawned;
        feats[1] = screwsCollected;
        feats[2] = forestFishCollisions;
        feats[3] = forestOutOfBounds;
        feats[4] = forestCollected5;
        feats[5] = forestApproaches;
        feats[6] = System.Convert.ToDouble(targetRtMedian);
        feats[7] = System.Convert.ToDouble(targetRtCv);
        feats[8] = forestKeyCount;
        feats[9] = System.Convert.ToDouble(forestKeyGapMean);
        feats[10] = System.Convert.ToDouble(forestKeyGapCv);
        feats[11] = riverFishCollisions;
        feats[12] = riverOutOfBounds;
        feats[13] = riverWins;
        feats[14] = riverApproaches;
        feats[15] = riverKeyCount;
        feats[16] = System.Convert.ToDouble(riverKeyGapMean);
        feats[17] = System.Convert.ToDouble(riverKeyGapCv);
        feats[18] = dwarfEnters;
        feats[19] = System.Convert.ToDouble(dwarfWmRtMedian);
        feats[20] = System.Convert.ToDouble(dwarfWmRtCv);
        feats[21] = dwarfCorrect;
        feats[22] = dwarfIncorrect;
        return feats;
    }

    private IEnumerator SendFeaturesToModel(double[] features)
    {
        if (features == null || features.Length == 0)
            yield break;

        // Build JSON: { "data": [ [v1, v2, ...] ], (optional) "fn_index": 0 }
        var sb = new StringBuilder();
        sb.Append('{');
        sb.Append("\"data\":[[");
        for (int i = 0; i < features.Length; i++)
        {
            if (i > 0) sb.Append(',');
            sb.Append(features[i].ToString("0.############", CultureInfo.InvariantCulture));
        }
        sb.Append("]] ");

        // If using HF Router, some deployments may expect fn_index; add it when URL suggests router usage
        bool isRouter = !string.IsNullOrWhiteSpace(modelApiUrl) && modelApiUrl.IndexOf("router.huggingface.co", StringComparison.OrdinalIgnoreCase) >= 0;
        if (isRouter)
        {
            sb.Append(',');
            sb.Append("\"fn_index\":0");
        }
        sb.Append('}');
        string json = sb.ToString();

        using (var req = new UnityWebRequest(modelApiUrl, "POST"))
        {
            byte[] bodyRaw = Encoding.UTF8.GetBytes(json);
            req.uploadHandler = new UploadHandlerRaw(bodyRaw);
            req.downloadHandler = new DownloadHandlerBuffer();
            req.SetRequestHeader("Content-Type", "application/json");
            if (!string.IsNullOrWhiteSpace(modelApiBearerToken))
            {
                req.SetRequestHeader("Authorization", "Bearer " + modelApiBearerToken);
            }
            req.timeout = 20; // seconds

            Debug.Log($"[ExperimentLogger] Sending {features.Length} features to model API: {modelApiUrl}");
            yield return req.SendWebRequest();

            if (req.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError($"[ExperimentLogger] Model API error: {req.responseCode} {req.error}\n{req.downloadHandler.text}");
            }
            else
            {
                string resp = req.downloadHandler.text;
                if (!string.IsNullOrEmpty(resp) && resp.Length > 500) resp = resp.Substring(0, 500) + "...";
                Debug.Log($"[ExperimentLogger] Model API response ({req.responseCode}): {resp}");

                // Try to extract a concise prediction text from the response and show it on screen
                string display = ExtractPredictionText(resp);
                if (!string.IsNullOrWhiteSpace(display))
                {
                    ShowPredictionOnScreen(display);
                }
            }
        }
    }

    // Dedicated parser for our model API schema:
    // {"prediction":[0|1], "probabilities":[[p0, p1]]}
    // Returns two-line UI text: "(No) ADHD detected.\nThe probability is: <p>"
    private string TryParseModelApiV1(string json)
    {
        if (string.IsNullOrWhiteSpace(json)) return null;

        try
        {
            // Find prediction array first element
            int pred = -1;
            {
                var m = System.Text.RegularExpressions.Regex.Match(
                    json,
                    @"""prediction""\s*:\s*\[\s*(?<pred>[-+]?[0-9]+)\s*\]",
                    System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                if (m.Success)
                {
                    int.TryParse(m.Groups["pred"].Value, out pred);
                }
            }

            if (pred != 0 && pred != 1)
            {
                return null; // not our shape
            }

            // Find probabilities first row two values
            double p0 = double.NaN, p1 = double.NaN;
            {
                var m = System.Text.RegularExpressions.Regex.Match(
                    json,
                    @"""probabilities""\s*:\s*\[\s*\[\s*(?<p0>[-+]?(?:[0-9]*\.?[0-9]+)(?:[eE][-+]?[0-9]+)?)\s*,\s*(?<p1>[-+]?(?:[0-9]*\.?[0-9]+)(?:[eE][-+]?[0-9]+)?)\s*\]",
                    System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                if (m.Success)
                {
                    double.TryParse(m.Groups["p0"].Value, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out p0);
                    double.TryParse(m.Groups["p1"].Value, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out p1);
                }
            }

            if (double.IsNaN(p0) || double.IsNaN(p1))
            {
                return null; // missing probabilities
            }

            double p = (pred == 0) ? p0 : p1;
            string label = (pred == 0) ? "No ADHD detected." : "ADHD detected.";
            string probText = p.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture);

            return label + "\n" + "the probability is: " + probText;
        }
        catch
        {
            return null;
        }
    }

    // Heuristic parser to extract a displayable prediction from various JSON response shapes
    private string ExtractPredictionText(string resp)
    {
        if (string.IsNullOrWhiteSpace(resp)) return null;

        string trimmed = resp.Trim();

        // First, try our known schema used by the deployed model API
        string v1 = TryParseModelApiV1(trimmed);
        if (!string.IsNullOrEmpty(v1)) return v1;

        // If plain number or string (not JSON), just show trimmed
        if (!(trimmed.StartsWith("{") || trimmed.StartsWith("[")))
        {
            return $"Prediction: {SafeTrimForUi(trimmed, 120)}";
        }

        // Try to fetch common keys from JSON text without heavy parsers
        string label = TryGetJsonStringValue(trimmed, "label") ?? TryGetJsonStringValue(trimmed, "prediction") ?? TryGetJsonStringValue(trimmed, "pred") ?? TryGetJsonStringValue(trimmed, "class") ?? TryGetJsonStringValue(trimmed, "result");
        string probNum = TryGetJsonNumberValue(trimmed, "probability") ?? TryGetJsonNumberValue(trimmed, "score") ?? TryGetJsonNumberValue(trimmed, "confidence") ?? TryGetJsonNumberValue(trimmed, "prob");

        if (!string.IsNullOrWhiteSpace(label) && !string.IsNullOrWhiteSpace(probNum))
        {
            return $"Prediction: {label} (p={probNum})";
        }
        if (!string.IsNullOrWhiteSpace(label))
        {
            return $"Prediction: {label}";
        }

        // If there is a top-level numeric field called prediction
        string predNum = TryGetJsonNumberValue(trimmed, "prediction") ?? TryGetJsonNumberValue(trimmed, "pred");
        if (!string.IsNullOrWhiteSpace(predNum))
        {
            return $"Prediction: {predNum}";
        }

        // As a fallback, show a short, safe snippet of the raw JSON
        return $"Prediction: {SafeTrimForUi(trimmed, 120)}";
    }

    private string SafeTrimForUi(string s, int max)
    {
        if (string.IsNullOrEmpty(s)) return s;
        s = s.Replace('\n', ' ').Replace('\r', ' ');
        if (s.Length > max) return s.Substring(0, max) + "...";
        return s;
    }

    // Very lightweight JSON helpers (string search) to avoid adding dependencies
    private string TryGetJsonStringValue(string json, string key)
    {
        try
        {
            int idx = json.IndexOf("\"" + key + "\"", StringComparison.OrdinalIgnoreCase);
            if (idx < 0) return null;
            idx = json.IndexOf(':', idx);
            if (idx < 0) return null;
            // Skip whitespace
            idx++;
            while (idx < json.Length && char.IsWhiteSpace(json[idx])) idx++;
            if (idx >= json.Length) return null;
            if (json[idx] == '"')
            {
                int start = ++idx;
                var sb = new System.Text.StringBuilder();
                while (idx < json.Length)
                {
                    char c = json[idx++];
                    if (c == '\\')
                    {
                        if (idx < json.Length)
                        {
                            char esc = json[idx++];
                            // handle minimal escapes
                            if (esc == '"' || esc == '\\' || esc == '/') sb.Append(esc);
                            else if (esc == 'b') sb.Append('\b');
                            else if (esc == 'f') sb.Append('\f');
                            else if (esc == 'n') sb.Append('\n');
                            else if (esc == 'r') sb.Append('\r');
                            else if (esc == 't') sb.Append('\t');
                            else if (esc == 'u' && idx + 3 < json.Length)
                            {
                                string hex = json.Substring(idx, 4);
                                if (ushort.TryParse(hex, System.Globalization.NumberStyles.HexNumber, null, out var code))
                                    sb.Append((char)code);
                                idx += 4;
                            }
                        }
                    }
                    else if (c == '"')
                    {
                        break;
                    }
                    else sb.Append(c);
                }
                return sb.ToString();
            }
        }
        catch { }
        return null;
    }

    private string TryGetJsonNumberValue(string json, string key)
    {
        try
        {
            int idx = json.IndexOf("\"" + key + "\"", StringComparison.OrdinalIgnoreCase);
            if (idx < 0) return null;
            idx = json.IndexOf(':', idx);
            if (idx < 0) return null;
            idx++;
            while (idx < json.Length && char.IsWhiteSpace(json[idx])) idx++;
            if (idx >= json.Length) return null;
            int start = idx;
            // capture until delimiter
            while (idx < json.Length)
            {
                char c = json[idx];
                if ((c >= '0' && c <= '9') || c == '.' || c == '-' || c == 'e' || c == 'E') { idx++; continue; }
                break;
            }
            if (idx <= start) return null;
            string token = json.Substring(start, idx - start);
            // validate numeric
            if (double.TryParse(token, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out double val))
            {
                return val.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture);
            }
        }
        catch { }
        return null;
    }

    // Path of the last session file written to disk (read by the clinician report).
    public string LastLocalSessionPath { get; private set; }

    private void SaveSessionLocalNow()
    {
        try
        {
            string dir = Path.Combine(Application.persistentDataPath, "session_logs");
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
            string path = Path.Combine(dir, $"{session.sessionId}.json");
            // Save the export JSON (scenes keyed by scene name) for readability
            string json = BuildExportJson(true);
            // Prepend a one-line summary: screws spawned/collected, forest & river metrics, and Target RT metrics
            int screwsSpawned = 0, screwsCollected = 0;
            int forestFishCollisions = 0, forestApproaches = 0, forestOutOfBounds = 0, forestCollected5 = 0;
            float targetRtMedian = 0f, targetRtCv = 0f;
            int forestKeyCount = 0; float forestKeyGapMean = 0f, forestKeyGapCv = 0f;
            int riverFishCollisions = 0, riverApproaches = 0, riverOutOfBounds = 0, riverWins = 0;
            int riverKeyCount = 0; float riverKeyGapMean = 0f, riverKeyGapCv = 0f;
            try { ComputeScrewCounts(out screwsSpawned, out screwsCollected); } catch { }
            try { ComputeForestFishCollisionAndApproachCounts(out forestFishCollisions, out forestApproaches); } catch { }
            try { ComputeForestOutOfBoundsCount(out forestOutOfBounds); } catch { }
            try { ComputeForestCollected5ScrewsCount(out forestCollected5); } catch { }
            try { ComputeScrewTargetRTMetrics(out targetRtMedian, out targetRtCv); } catch { }
            try { ComputeForestKeyPressMetrics(out forestKeyCount, out forestKeyGapMean, out forestKeyGapCv); } catch { }
            try { ComputeRiverFishCollisionAndApproachCounts(out riverFishCollisions, out riverApproaches); } catch { }
            try { ComputeRiverOutOfBoundsCount(out riverOutOfBounds); } catch { }
            try { ComputeRiverWinCount(out riverWins); } catch { }
            try { ComputeRiverKeyPressMetrics(out riverKeyCount, out riverKeyGapMean, out riverKeyGapCv); } catch { }
            // Dwarf/Memory metrics
            int dwarfEnters = 0; float dwarfWmRtMedian = 0f, dwarfWmRtCv = 0f; int dwarfCorrect = 0, dwarfIncorrect = 0;
            try { ComputeDwarfEnterCount(out dwarfEnters); } catch { }
            try { ComputeDwarfWmRtMetrics(out dwarfWmRtMedian, out dwarfWmRtCv); } catch { }
            try { ComputeDwarfCorrectIncorrectCounts(out dwarfCorrect, out dwarfIncorrect); } catch { }
            string header = $"screws_spawned={screwsSpawned}, screws_collected={screwsCollected}, " +
                           $"forest_fish_collisions={forestFishCollisions}, forest_out_of_bounds={forestOutOfBounds}, forest_5_screws_collected={forestCollected5}, forest_approaches={forestApproaches}, " +
                           $"target_rt_median={R2(targetRtMedian)}, target_rt_cv={R2(targetRtCv)}, " +
                           $"forest_keys={forestKeyCount}, forest_key_gap_mean={R2(forestKeyGapMean)}, forest_key_gap_cv={R2(forestKeyGapCv)}, " +
                           $"river_fish_collisions={riverFishCollisions}, river_out_of_bounds={riverOutOfBounds}, river_win={riverWins}, river_approaches={riverApproaches}, " +
                           $"river_keys={riverKeyCount}, river_key_gap_mean={R2(riverKeyGapMean)}, river_key_gap_cv={R2(riverKeyGapCv)}, " +
                           $"dwarf_enters={dwarfEnters}, dwarf_wm_rt_median={R2(dwarfWmRtMedian)}, dwarf_wm_rt_cv={R2(dwarfWmRtCv)}, " +
                           $"dwarf_correct={dwarfCorrect}, dwarf_incorrect={dwarfIncorrect}";
            string content = header + "\n" + json;
            File.WriteAllText(path, content, Encoding.UTF8);
            LastLocalSessionPath = path;
            Debug.Log($"[ExperimentLogger] Session saved locally at {path}");
        }
        catch (Exception ex)
        {
            Debug.LogError($"[ExperimentLogger] Failed to save locally: {ex.Message}");
        }
    }

    [Serializable]
    public class SupabaseSessionRow
    {
        public string player_name;
        public string session_id;
        public SessionData session_json;
    }

    // ------- Export helpers to produce a scenes-keyed JSON -------
    private string BuildSupabaseRowJson(string sessionExportJson)
    {
        // Compose the row manually so that session_json uses our custom export JSON
        var sb = new StringBuilder();
        sb.Append('{');
        sb.Append("\"player_name\":\"").Append(EscapeJsonString(playerName ?? string.Empty)).Append('\"');
        sb.Append(',');
        sb.Append("\"session_id\":\"").Append(EscapeJsonString(session?.sessionId ?? string.Empty)).Append('\"');
        sb.Append(',');
        sb.Append("\"session_json\":");
        sb.Append(string.IsNullOrEmpty(sessionExportJson) ? "{}" : sessionExportJson);
        sb.Append('}');
        return sb.ToString();
    }

    private string BuildExportJson(bool pretty)
    {
        if (session == null)
            return "{}";

        var sb = new StringBuilder();
        string indent = pretty ? "  " : string.Empty;
        string nl = pretty ? "\n" : string.Empty;
        int level = 0;

        void AppendIndent(int lvl)
        {
            if (!pretty) return;
            for (int i = 0; i < lvl; i++) sb.Append(indent);
        }

        void AppendQuoted(string s)
        {
            sb.Append('"').Append(EscapeJsonString(s)).Append('"');
        }

        string ListToJson<T>(System.Collections.Generic.List<T> list)
        {
            if (list == null || list.Count == 0) return "[]";
            var ls = new StringBuilder();
            ls.Append('[');
            for (int i = 0; i < list.Count; i++)
            {
                if (i > 0) ls.Append(',');
                ls.Append(JsonUtility.ToJson(list[i], false));
            }
            ls.Append(']');
            return ls.ToString();
        }

        string MinigamesToJson(System.Collections.Generic.List<MinigameSection> mgs)
        {
            if (mgs == null || mgs.Count == 0) return "[]";
            var ls = new StringBuilder();
            ls.Append('[');
            for (int i = 0; i < mgs.Count; i++)
            {
                if (i > 0) ls.Append(',');
                // Serialize each minigame as-is (includes name and summary fields)
                ls.Append(JsonUtility.ToJson(mgs[i], false));
            }
            ls.Append(']');
            return ls.ToString();
        }

        sb.Append('{').Append(nl); level++;
        // Add a human-readable summary line as the first property of the JSON sent to Supabase
        int _screwsSpawned = 0, _screwsCollected = 0;
        int _forestFishCollisions = 0, _forestApproaches = 0, _forestOutOfBounds = 0, _forestCollected5 = 0;
        float _targetRtMedian = 0f, _targetRtCv = 0f;
        int _forestKeyCount = 0; float _forestKeyGapMean = 0f, _forestKeyGapCv = 0f;
        int _riverFishCollisions = 0, _riverApproaches = 0, _riverOutOfBounds = 0, _riverWins = 0;
        int _riverKeyCount = 0; float _riverKeyGapMean = 0f, _riverKeyGapCv = 0f;
        int _dwarfEnters = 0; float _dwarfWmRtMedian = 0f, _dwarfWmRtCv = 0f;
        int _dwarfCorrect = 0, _dwarfIncorrect = 0;
        try { ComputeScrewCounts(out _screwsSpawned, out _screwsCollected); } catch { }
        try { ComputeForestFishCollisionAndApproachCounts(out _forestFishCollisions, out _forestApproaches); } catch { }
        try { ComputeForestOutOfBoundsCount(out _forestOutOfBounds); } catch { }
        try { ComputeForestCollected5ScrewsCount(out _forestCollected5); } catch { }
        try { ComputeScrewTargetRTMetrics(out _targetRtMedian, out _targetRtCv); } catch { }
        try { ComputeForestKeyPressMetrics(out _forestKeyCount, out _forestKeyGapMean, out _forestKeyGapCv); } catch { }
        try { ComputeRiverFishCollisionAndApproachCounts(out _riverFishCollisions, out _riverApproaches); } catch { }
        try { ComputeRiverOutOfBoundsCount(out _riverOutOfBounds); } catch { }
        try { ComputeRiverWinCount(out _riverWins); } catch { }
        try { ComputeRiverKeyPressMetrics(out _riverKeyCount, out _riverKeyGapMean, out _riverKeyGapCv); } catch { }
        try { ComputeDwarfEnterCount(out _dwarfEnters); } catch { }
        try { ComputeDwarfWmRtMetrics(out _dwarfWmRtMedian, out _dwarfWmRtCv); } catch { }
        try { ComputeDwarfCorrectIncorrectCounts(out _dwarfCorrect, out _dwarfIncorrect); } catch { }
        string _summaryLine = $"screws_spawned={_screwsSpawned}, screws_collected={_screwsCollected}, " +
                              $"forest_fish_collisions={_forestFishCollisions}, forest_out_of_bounds={_forestOutOfBounds}, forest_5_screws_collected={_forestCollected5}, forest_approaches={_forestApproaches}, " +
                              $"target_rt_median={R2(_targetRtMedian)}, target_rt_cv={R2(_targetRtCv)}, " +
                              $"forest_keys={_forestKeyCount}, forest_key_gap_mean={R2(_forestKeyGapMean)}, forest_key_gap_cv={R2(_forestKeyGapCv)}, " +
                              $"river_fish_collisions={_riverFishCollisions}, river_out_of_bounds={_riverOutOfBounds}, river_win={_riverWins}, river_approaches={_riverApproaches}, " +
                              $"river_keys={_riverKeyCount}, river_key_gap_mean={R2(_riverKeyGapMean)}, river_key_gap_cv={R2(_riverKeyGapCv)}, " +
                              $"dwarf_enters={_dwarfEnters}, dwarf_wm_rt_median={R2(_dwarfWmRtMedian)}, dwarf_wm_rt_cv={R2(_dwarfWmRtCv)}, " +
                              $"dwarf_correct={_dwarfCorrect}, dwarf_incorrect={_dwarfIncorrect}";
        AppendIndent(level); AppendQuoted("summaryLine"); sb.Append(':'); AppendQuoted(_summaryLine); sb.Append(',').Append(nl);
        AppendIndent(level); AppendQuoted("sessionId"); sb.Append(':'); AppendQuoted(session.sessionId ?? ""); sb.Append(',').Append(nl);
        AppendIndent(level); AppendQuoted("playerName"); sb.Append(':'); AppendQuoted(session.playerName ?? ""); sb.Append(',').Append(nl);
        AppendIndent(level); AppendQuoted("startTime"); sb.Append(':'); AppendQuoted(session.startTime ?? ""); sb.Append(',').Append(nl);
        AppendIndent(level); AppendQuoted("endTime"); sb.Append(':'); AppendQuoted(session.endTime ?? ""); sb.Append(',').Append(nl);

        // scenes as an object keyed by scene name
        AppendIndent(level); AppendQuoted("scenes"); sb.Append(':'); sb.Append('{').Append(nl); level++;
        if (session.scenes != null)
        {
            for (int i = 0; i < session.scenes.Count; i++)
            {
                var scene = session.scenes[i];
                AppendIndent(level);
                AppendQuoted(scene.scene ?? $"scene_{i}"); sb.Append(':'); sb.Append('{'); sb.Append(nl); level++;

                // events
                AppendIndent(level); AppendQuoted("events"); sb.Append(':'); sb.Append(ListToJson(scene.events)); sb.Append(',').Append(nl);
                // keys
                AppendIndent(level); AppendQuoted("keys"); sb.Append(':'); sb.Append(ListToJson(scene.keys)); sb.Append(',').Append(nl);
                // outcomes
                AppendIndent(level); AppendQuoted("outcomes"); sb.Append(':'); sb.Append(ListToJson(scene.outcomes)); sb.Append(',').Append(nl);
                // river trial logs (river is not a minigame; stored directly under scene section)
                AppendIndent(level); AppendQuoted("riverJumpStarts"); sb.Append(':'); sb.Append(ListToJson(scene.riverJumpStarts)); sb.Append(',').Append(nl);
                AppendIndent(level); AppendQuoted("riverJumpLands"); sb.Append(':'); sb.Append(ListToJson(scene.riverJumpLands)); sb.Append(',').Append(nl);
                AppendIndent(level); AppendQuoted("riverCollisions"); sb.Append(':'); sb.Append(ListToJson(scene.riverCollisions)); sb.Append(',').Append(nl);
                AppendIndent(level); AppendQuoted("riverFinish"); sb.Append(':'); sb.Append(ListToJson(scene.riverFinish)); sb.Append(',').Append(nl);
                // fish spawns
                AppendIndent(level); AppendQuoted("fishSpawns"); sb.Append(':'); sb.Append(ListToJson(scene.fishSpawns)); sb.Append(',').Append(nl);
                // proximity approaches (player moving towards fish/blueberry)
                AppendIndent(level); AppendQuoted("approaches"); sb.Append(':'); sb.Append(ListToJson(scene.approaches)); sb.Append(',').Append(nl);
                // minigames
                AppendIndent(level); AppendQuoted("minigames"); sb.Append(':'); sb.Append(MinigamesToJson(scene.minigames)); sb.Append(nl);

                level--; AppendIndent(level); sb.Append('}');
                if (i < session.scenes.Count - 1) sb.Append(',');
                sb.Append(nl);
            }
        }
        // Close the scenes object; do NOT append a trailing comma since no fields follow
        level--; AppendIndent(level); sb.Append('}'); sb.Append(nl);

        // Flat sceneEvents removed per request; scene events are listed under each scene

        level--; AppendIndent(level); sb.Append('}');
        return sb.ToString();
    }

    // Computes total screws spawned and collected across the entire session
    private void ComputeScrewCounts(out int spawned, out int collected)
    {
        spawned = 0;
        collected = 0;
        try
        {
            if (session?.scenes == null) return;
            for (int i = 0; i < session.scenes.Count; i++)
            {
                var scene = session.scenes[i];
                if (scene?.minigames == null) continue;
                for (int j = 0; j < scene.minigames.Count; j++)
                {
                    var mg = scene.minigames[j];
                    if (mg == null) continue;
                    // Spawns
                    if (mg.forestItemSpawns != null)
                    {
                        for (int k = 0; k < mg.forestItemSpawns.Count; k++)
                        {
                            var s = mg.forestItemSpawns[k];
                            if (s != null && string.Equals(s.itemType, "screw", StringComparison.OrdinalIgnoreCase))
                                spawned++;
                        }
                    }
                    // Resolves (collected only)
                    if (mg.forestItemResolves != null)
                    {
                        for (int k = 0; k < mg.forestItemResolves.Count; k++)
                        {
                            var r = mg.forestItemResolves[k];
                            if (r != null
                                && string.Equals(r.itemType, "screw", StringComparison.OrdinalIgnoreCase)
                                && string.Equals(r.resolution, "collected", StringComparison.OrdinalIgnoreCase))
                                collected++;
                        }
                    }
                }
            }
        }
        catch { }
    }

    // Computes how many times the user entered the Dwarf (Memory) scene by counting
    // scene_enter events under the grouped 'dwarf' scene section.
    private void ComputeDwarfEnterCount(out int enters)
    {
        enters = 0;
        try
        {
            if (session?.scenes == null) return;

            // Preferred: count Memory minigame starts (represents actual plays)
            int starts = 0;
            for (int i = 0; i < session.scenes.Count; i++)
            {
                var scene = session.scenes[i];
                if (scene == null) continue;
                if (!string.Equals(scene.scene, "dwarf", StringComparison.OrdinalIgnoreCase)) continue;
                if (scene.minigames == null) continue;
                for (int j = 0; j < scene.minigames.Count; j++)
                {
                    var mg = scene.minigames[j];
                    if (mg == null) continue;
                    if (!string.Equals(mg.name, "memory", StringComparison.OrdinalIgnoreCase)) continue;
                    if (mg.events == null) continue;
                    for (int k = 0; k < mg.events.Count; k++)
                    {
                        var ev = mg.events[k];
                        if (ev == null) continue;
                        if (string.Equals(ev.eventType, "start", StringComparison.OrdinalIgnoreCase))
                        {
                            starts++;
                        }
                    }
                }
            }
            if (starts > 0)
            {
                enters = starts;
                return;
            }

            // Fallback: count unique dwarf scene enters by distinct sceneVisitId
            var ids = new System.Collections.Generic.HashSet<string>();
            for (int i = 0; i < session.scenes.Count; i++)
            {
                var scene = session.scenes[i];
                if (scene == null) continue;
                if (!string.Equals(scene.scene, "dwarf", StringComparison.OrdinalIgnoreCase)) continue;
                if (scene.events == null) continue;
                for (int j = 0; j < scene.events.Count; j++)
                {
                    var e = scene.events[j];
                    if (e == null) continue;
                    if (!string.Equals(e.eventType, "scene_enter", StringComparison.OrdinalIgnoreCase)) continue;
                    if (!string.IsNullOrEmpty(e.sceneVisitId)) ids.Add(e.sceneVisitId);
                }
            }
            enters = ids.Count;
        }
        catch { }
    }

    // Working Memory (Memory minigame) RT metrics in the Dwarf scene.
    // Compute RTs as gaps between consecutive left-button DOWN mouse inputs within the
    // 'memory' minigame under the 'dwarf' scene, using tScene (time since scene start).
    private void ComputeDwarfWmRtMetrics(out float median, out float cv)
    {
        median = 0f;
        cv = 0f;
        try
        {
            if (session?.scenes == null) return;
            var times = new System.Collections.Generic.List<float>(256);
            // Primary source: mouseInputs (left/down) during memory
            for (int i = 0; i < session.scenes.Count; i++)
            {
                var scene = session.scenes[i];
                if (scene == null) continue;
                if (!string.Equals(scene.scene, "dwarf", StringComparison.OrdinalIgnoreCase)) continue;
                if (scene.minigames == null) continue;
                for (int j = 0; j < scene.minigames.Count; j++)
                {
                    var mg = scene.minigames[j];
                    if (mg == null) continue;
                    if (!string.Equals(mg.name, "memory", StringComparison.OrdinalIgnoreCase)) continue;
                    if (mg.mouseInputs != null)
                    {
                        for (int k = 0; k < mg.mouseInputs.Count; k++)
                        {
                            var mi = mg.mouseInputs[k];
                            if (mi == null) continue;
                            if (!string.Equals(mi.button, "left", StringComparison.OrdinalIgnoreCase)) continue;
                            if (!string.Equals(mi.action, "down", StringComparison.OrdinalIgnoreCase)) continue;
                            float t = mi.tScene;
                            if (!float.IsNaN(t) && !float.IsInfinity(t))
                            {
                                times.Add(t);
                            }
                        }
                    }
                }
            }

            // Fallback: use memory clicks if mouseInputs insufficient
            if (times.Count < 2)
            {
                times.Clear();
                for (int i = 0; i < session.scenes.Count; i++)
                {
                    var scene = session.scenes[i];
                    if (scene == null) continue;
                    if (!string.Equals(scene.scene, "dwarf", StringComparison.OrdinalIgnoreCase)) continue;
                    if (scene.minigames == null) continue;
                    for (int j = 0; j < scene.minigames.Count; j++)
                    {
                        var mg = scene.minigames[j];
                        if (mg == null) continue;
                        if (!string.Equals(mg.name, "memory", StringComparison.OrdinalIgnoreCase)) continue;
                        if (mg.clicks == null) continue;
                        for (int k = 0; k < mg.clicks.Count; k++)
                        {
                            var c = mg.clicks[k];
                            if (c == null) continue;
                            float t = c.timeSinceSceneStart;
                            if (!float.IsNaN(t) && !float.IsInfinity(t))
                            {
                                times.Add(t);
                            }
                        }
                    }
                }
            }

            if (times.Count < 2)
            {
                median = 0f;
                cv = 0f;
                return;
            }

            times.Sort();
            var rts = new System.Collections.Generic.List<float>(times.Count - 1);
            for (int i = 1; i < times.Count; i++)
            {
                float dt = times[i] - times[i - 1];
                if (dt > 0f && !float.IsNaN(dt) && !float.IsInfinity(dt))
                {
                    rts.Add(dt);
                }
            }

            if (rts.Count == 0)
            {
                median = 0f;
                cv = 0f;
                return;
            }

            rts.Sort();
            int n = rts.Count;
            if ((n & 1) == 1)
            {
                median = rts[n / 2];
            }
            else
            {
                median = (rts[(n / 2) - 1] + rts[n / 2]) * 0.5f;
            }

            double sum = 0.0;
            for (int i = 0; i < n; i++) sum += rts[i];
            double mean = sum / n;
            if (mean == 0.0)
            {
                cv = 0f;
                return;
            }
            double varSum = 0.0;
            for (int i = 0; i < n; i++)
            {
                double d = rts[i] - mean;
                varSum += d * d;
            }
            double variance = varSum / n; // population variance
            double sd = Math.Sqrt(variance);
            cv = (float)(sd / mean);
        }
        catch
        {
            median = 0f;
            cv = 0f;
        }
    }

    // Computes counts scoped to the Forest scene: number of fish collisions (outcomes with reason == "fish_collision")
    // and number of approach_target events under the Forest section.
    private void ComputeForestFishCollisionAndApproachCounts(out int fishCollisions, out int approaches)
    {
        fishCollisions = 0;
        approaches = 0;
        try
        {
            if (session?.scenes == null) return;
            for (int i = 0; i < session.scenes.Count; i++)
            {
                var scene = session.scenes[i];
                if (scene == null) continue;
                if (!string.Equals(scene.scene, "forest", StringComparison.OrdinalIgnoreCase)) continue;

                // Count outcomes with reason == fish_collision
                if (scene.outcomes != null)
                {
                    for (int j = 0; j < scene.outcomes.Count; j++)
                    {
                        var o = scene.outcomes[j];
                        if (o == null) continue;
                        if (string.Equals(o.reason, "fish_collision", StringComparison.OrdinalIgnoreCase))
                        {
                            fishCollisions++;
                        }
                    }
                }
                // Count approach events
                if (scene.approaches != null)
                {
                    approaches += scene.approaches.Count;
                }
            }
        }
        catch { }
    }

    // Count correct and incorrect answers in the Dwarf (Memory) minigame
    private void ComputeDwarfCorrectIncorrectCounts(out int correct, out int incorrect)
    {
        correct = 0;
        incorrect = 0;
        try
        {
            if (session?.scenes == null) return;
            for (int i = 0; i < session.scenes.Count; i++)
            {
                var scene = session.scenes[i];
                if (scene == null) continue;
                if (!string.Equals(scene.scene, "dwarf", StringComparison.OrdinalIgnoreCase)) continue;
                if (scene.minigames == null) continue;
                for (int j = 0; j < scene.minigames.Count; j++)
                {
                    var mg = scene.minigames[j];
                    if (mg == null) continue;
                    if (!string.Equals(mg.name, "memory", StringComparison.OrdinalIgnoreCase)) continue;
                    if (mg.dwarfTrialResponses == null) continue;
                    for (int k = 0; k < mg.dwarfTrialResponses.Count; k++)
                    {
                        var resp = mg.dwarfTrialResponses[k];
                        if (resp == null) continue;
                        if (resp.isCorrect) correct++; else incorrect++;
                    }
                }
            }
        }
        catch { }
    }

    // Computes how many times the player went out of bounds in the Forest scene
    // by counting outcomes under the Forest section with reason == "out_of_bounds".
    private void ComputeForestOutOfBoundsCount(out int outOfBounds)
    {
        outOfBounds = 0;
        try
        {
            if (session?.scenes == null) return;
            for (int i = 0; i < session.scenes.Count; i++)
            {
                var scene = session.scenes[i];
                if (scene == null) continue;
                if (!string.Equals(scene.scene, "forest", StringComparison.OrdinalIgnoreCase)) continue;

                if (scene.outcomes != null)
                {
                    for (int j = 0; j < scene.outcomes.Count; j++)
                    {
                        var o = scene.outcomes[j];
                        if (o == null) continue;
                        if (string.Equals(o.reason, "out_of_bounds", StringComparison.OrdinalIgnoreCase))
                        {
                            outOfBounds++;
                        }
                    }
                }
            }
        }
        catch { }
    }

    // Computes how many Forest outcomes were wins due to collecting 5 screws.
    // Count outcomes under the Forest scene where reason indicates all screws were collected.
    // Accept both "collected_5_screws" and "collected_all_screws" (case-insensitive).
    private void ComputeForestCollected5ScrewsCount(out int collected5)
    {
        collected5 = 0;
        try
        {
            if (session?.scenes == null) return;
            for (int i = 0; i < session.scenes.Count; i++)
            {
                var scene = session.scenes[i];
                if (scene == null) continue;
                if (!string.Equals(scene.scene, "forest", StringComparison.OrdinalIgnoreCase)) continue;

                if (scene.outcomes != null)
                {
                    for (int j = 0; j < scene.outcomes.Count; j++)
                    {
                        var o = scene.outcomes[j];
                        if (o == null) continue;
                        string reasonLower = (o.reason ?? string.Empty).ToLowerInvariant();
                        if (reasonLower == "collected_5_screws" || reasonLower == "collected_all_screws")
                        {
                            collected5++;
                        }
                    }
                }
            }
        }
        catch { }
    }

    // Compute Forest key press metrics:
    // - keyCount: number of keyboard "down" actions logged under the Forest scene
    // - gapMean: average time (in seconds) between consecutive key downs (across the whole session)
    // - gapCv: coefficient of variation of the gaps = SD(gap)/Mean(gap) using population SD
    private void ComputeForestKeyPressMetrics(out int keyCount, out float gapMean, out float gapCv)
    {
        keyCount = 0;
        gapMean = 0f;
        gapCv = 0f;
        try
        {
            if (session?.scenes == null) return;
            var times = new System.Collections.Generic.List<float>(256);
            for (int i = 0; i < session.scenes.Count; i++)
            {
                var scene = session.scenes[i];
                if (scene == null) continue;
                if (!string.Equals(scene.scene, "forest", StringComparison.OrdinalIgnoreCase)) continue;
                if (scene.keys == null) continue;
                for (int j = 0; j < scene.keys.Count; j++)
                {
                    var k = scene.keys[j];
                    if (k == null) continue;
                    if (!string.Equals(k.action, "down", StringComparison.OrdinalIgnoreCase)) continue;
                    // Use timeSinceStart so ordering is consistent across multiple forest visits
                    float t = k.timeSinceStart;
                    if (!float.IsNaN(t) && !float.IsInfinity(t))
                    {
                        times.Add(t);
                    }
                }
            }

            keyCount = times.Count;
            if (times.Count < 2)
            {
                gapMean = 0f;
                gapCv = 0f;
                return;
            }

            times.Sort();
            var gaps = new System.Collections.Generic.List<float>(times.Count - 1);
            for (int i = 1; i < times.Count; i++)
            {
                float dt = times[i] - times[i - 1];
                if (dt > 0f && !float.IsNaN(dt) && !float.IsInfinity(dt))
                {
                    gaps.Add(dt);
                }
            }

            if (gaps.Count == 0)
            {
                gapMean = 0f;
                gapCv = 0f;
                return;
            }

            // Mean
            double sum = 0.0;
            for (int i = 0; i < gaps.Count; i++) sum += gaps[i];
            double mean = sum / gaps.Count;
            gapMean = (float)mean;
            if (mean == 0.0)
            {
                gapCv = 0f;
                return;
            }
            // Population SD
            double varSum = 0.0;
            for (int i = 0; i < gaps.Count; i++)
            {
                double d = gaps[i] - mean;
                varSum += d * d;
            }
            double variance = varSum / gaps.Count;
            double sd = Math.Sqrt(variance);
            gapCv = (float)(sd / mean);
        }
        catch
        {
            keyCount = 0;
            gapMean = 0f;
            gapCv = 0f;
        }
    }

    // Computes counts scoped to the River scene: number of fish collisions (outcomes with reason == "fish_collision")
    // and number of approach_target events under the River section.
    private void ComputeRiverFishCollisionAndApproachCounts(out int fishCollisions, out int approaches)
    {
        fishCollisions = 0;
        approaches = 0;
        try
        {
            if (session?.scenes == null) return;
            for (int i = 0; i < session.scenes.Count; i++)
            {
                var scene = session.scenes[i];
                if (scene == null) continue;
                if (!string.Equals(scene.scene, "river", StringComparison.OrdinalIgnoreCase)) continue;

                // Count outcomes with reason == fish_collision
                if (scene.outcomes != null)
                {
                    for (int j = 0; j < scene.outcomes.Count; j++)
                    {
                        var o = scene.outcomes[j];
                        if (o == null) continue;
                        if (string.Equals(o.reason, "fish_collision", StringComparison.OrdinalIgnoreCase))
                        {
                            fishCollisions++;
                        }
                    }
                }
                // Count approach events
                if (scene.approaches != null)
                {
                    approaches += scene.approaches.Count;
                }
            }
        }
        catch { }
    }

    // Computes how many times the player went out of bounds in the River scene
    // by counting outcomes under the River section with reason == "out_of_bounds".
    private void ComputeRiverOutOfBoundsCount(out int outOfBounds)
    {
        outOfBounds = 0;
        try
        {
            if (session?.scenes == null) return;
            for (int i = 0; i < session.scenes.Count; i++)
            {
                var scene = session.scenes[i];
                if (scene == null) continue;
                if (!string.Equals(scene.scene, "river", StringComparison.OrdinalIgnoreCase)) continue;

                if (scene.outcomes != null)
                {
                    for (int j = 0; j < scene.outcomes.Count; j++)
                    {
                        var o = scene.outcomes[j];
                        if (o == null) continue;
                        if (string.Equals(o.reason, "out_of_bounds", StringComparison.OrdinalIgnoreCase))
                        {
                            outOfBounds++;
                        }
                    }
                }
            }
        }
        catch { }
    }

    // Computes how many River outcomes were wins (outcome == "win")
    private void ComputeRiverWinCount(out int wins)
    {
        wins = 0;
        try
        {
            if (session?.scenes == null) return;
            for (int i = 0; i < session.scenes.Count; i++)
            {
                var scene = session.scenes[i];
                if (scene == null) continue;
                if (!string.Equals(scene.scene, "river", StringComparison.OrdinalIgnoreCase)) continue;

                if (scene.outcomes != null)
                {
                    for (int j = 0; j < scene.outcomes.Count; j++)
                    {
                        var o = scene.outcomes[j];
                        if (o == null) continue;
                        if (string.Equals(o.outcome, "win", StringComparison.OrdinalIgnoreCase))
                        {
                            wins++;
                        }
                    }
                }
            }
        }
        catch { }
    }

    // Compute River key press metrics (analogous to Forest):
    // - keyCount: number of keyboard "down" actions logged under the River scene
    // - gapMean: average time (in seconds) between consecutive key downs (across the whole session)
    // - gapCv: coefficient of variation of the gaps = SD(gap)/Mean(gap) using population SD
    private void ComputeRiverKeyPressMetrics(out int keyCount, out float gapMean, out float gapCv)
    {
        keyCount = 0;
        gapMean = 0f;
        gapCv = 0f;
        try
        {
            if (session?.scenes == null) return;
            var times = new System.Collections.Generic.List<float>(256);
            for (int i = 0; i < session.scenes.Count; i++)
            {
                var scene = session.scenes[i];
                if (scene == null) continue;
                if (!string.Equals(scene.scene, "river", StringComparison.OrdinalIgnoreCase)) continue;
                if (scene.keys == null) continue;
                for (int j = 0; j < scene.keys.Count; j++)
                {
                    var k = scene.keys[j];
                    if (k == null) continue;
                    if (!string.Equals(k.action, "down", StringComparison.OrdinalIgnoreCase)) continue;
                    float t = k.timeSinceStart;
                    if (!float.IsNaN(t) && !float.IsInfinity(t))
                    {
                        times.Add(t);
                    }
                }
            }

            keyCount = times.Count;
            if (times.Count < 2)
            {
                gapMean = 0f;
                gapCv = 0f;
                return;
            }

            times.Sort();
            var gaps = new System.Collections.Generic.List<float>(times.Count - 1);
            for (int i = 1; i < times.Count; i++)
            {
                float dt = times[i] - times[i - 1];
                if (dt > 0f && !float.IsNaN(dt) && !float.IsInfinity(dt))
                {
                    gaps.Add(dt);
                }
            }

            if (gaps.Count == 0)
            {
                gapMean = 0f;
                gapCv = 0f;
                return;
            }

            // Mean
            double sum = 0.0;
            for (int i = 0; i < gaps.Count; i++) sum += gaps[i];
            double mean = sum / gaps.Count;
            gapMean = (float)mean;
            if (mean == 0.0)
            {
                gapCv = 0f;
                return;
            }
            // Population SD
            double varSum = 0.0;
            for (int i = 0; i < gaps.Count; i++)
            {
                double d = gaps[i] - mean;
                varSum += d * d;
            }
            double variance = varSum / gaps.Count;
            double sd = Math.Sqrt(variance);
            gapCv = (float)(sd / mean);
        }
        catch
        {
            keyCount = 0;
            gapMean = 0f;
            gapCv = 0f;
        }
    }

    // Compute Target Reaction Time metrics for screws: RT = resolve.tScene - spawn.tScene
    // Only include resolves where itemType == "screw" and resolution == "collected".
    // Matching by composite key sceneVisitId|itemId to ensure correct pairing across runs.
    private void ComputeScrewTargetRTMetrics(out float median, out float cv)
    {
        median = 0f;
        cv = 0f;
        try
        {
            if (session?.scenes == null)
                return;

            // Collect all screw spawns keyed by visit+itemId
            var spawnMap = new System.Collections.Generic.Dictionary<string, float>(1024);
            for (int i = 0; i < session.scenes.Count; i++)
            {
                var scene = session.scenes[i];
                if (scene?.minigames == null) continue;
                for (int j = 0; j < scene.minigames.Count; j++)
                {
                    var mg = scene.minigames[j];
                    if (mg?.forestItemSpawns == null) continue;
                    for (int k = 0; k < mg.forestItemSpawns.Count; k++)
                    {
                        var s = mg.forestItemSpawns[k];
                        if (s == null) continue;
                        if (!string.Equals(s.itemType, "screw", StringComparison.OrdinalIgnoreCase)) continue;
                        if (string.IsNullOrEmpty(s.sceneVisitId) || string.IsNullOrEmpty(s.itemId)) continue;
                        string key = s.sceneVisitId + "|" + s.itemId;
                        if (!spawnMap.ContainsKey(key))
                        {
                            spawnMap[key] = s.tScene;
                        }
                    }
                }
            }

            // Collect RTs for collected screws
            var rts = new System.Collections.Generic.List<float>(128);
            for (int i = 0; i < session.scenes.Count; i++)
            {
                var scene = session.scenes[i];
                if (scene?.minigames == null) continue;
                for (int j = 0; j < scene.minigames.Count; j++)
                {
                    var mg = scene.minigames[j];
                    if (mg?.forestItemResolves == null) continue;
                    for (int k = 0; k < mg.forestItemResolves.Count; k++)
                    {
                        var r = mg.forestItemResolves[k];
                        if (r == null) continue;
                        if (!string.Equals(r.itemType, "screw", StringComparison.OrdinalIgnoreCase)) continue;
                        if (!string.Equals(r.resolution, "collected", StringComparison.OrdinalIgnoreCase)) continue;
                        if (string.IsNullOrEmpty(r.sceneVisitId) || string.IsNullOrEmpty(r.itemId)) continue;
                        string key = r.sceneVisitId + "|" + r.itemId;
                        float tSpawn;
                        if (spawnMap.TryGetValue(key, out tSpawn))
                        {
                            float rt = r.tScene - tSpawn;
                            if (rt >= 0f && !float.IsNaN(rt) && !float.IsInfinity(rt))
                            {
                                rts.Add(rt);
                            }
                        }
                    }
                }
            }

            if (rts.Count == 0)
            {
                median = 0f;
                cv = 0f;
                return;
            }

            rts.Sort();
            int n = rts.Count;
            if ((n & 1) == 1)
            {
                median = rts[n / 2];
            }
            else
            {
                median = (rts[(n / 2) - 1] + rts[n / 2]) * 0.5f;
            }

            // Mean and standard deviation (population)
            double sum = 0.0;
            for (int i = 0; i < n; i++) sum += rts[i];
            double mean = sum / n;
            if (mean == 0.0)
            {
                cv = 0f;
                return;
            }
            double varSum = 0.0;
            for (int i = 0; i < n; i++)
            {
                double d = rts[i] - mean;
                varSum += d * d;
            }
            double variance = varSum / n; // population variance
            double sd = Math.Sqrt(variance);
            cv = (float)(sd / mean);
        }
        catch
        {
            median = 0f;
            cv = 0f;
        }
    }

    private string EscapeJsonString(string s)
    {
        if (string.IsNullOrEmpty(s)) return string.Empty;
        var sb = new StringBuilder();
        foreach (char c in s)
        {
            switch (c)
            {
                case '"': sb.Append("\\\""); break;
                case '\\': sb.Append("\\\\"); break;
                case '\b': sb.Append("\\b"); break;
                case '\f': sb.Append("\\f"); break;
                case '\n': sb.Append("\\n"); break;
                case '\r': sb.Append("\\r"); break;
                case '\t': sb.Append("\\t"); break;
                default:
                    if (c < 32)
                    {
                        sb.AppendFormat("\\u{0:X4}", (int)c);
                    }
                    else
                    {
                        sb.Append(c);
                    }
                    break;
            }
        }
        return sb.ToString();
    }
}
