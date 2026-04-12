namespace VisualizationService.Utilities
{
    public static class ParsingUtility
    {
        public static bool TryMapFieldName(string resourceName, out string fieldName)
        {
            fieldName = resourceName switch
            {
                "globalActivePower" => "globalActivePower",
                "voltage" => "voltage",
                "globalIntensity" => "globalIntensity",
                _ => null
            };

            return !string.IsNullOrEmpty(fieldName);
        }

        public static DateTime ConvertUnixNanosecondsToDateTime(long unixNanoseconds)
        {
            var epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            var ticks = unixNanoseconds / 100; // 1 tick = 100 ns
            return epoch.AddTicks(ticks);
        }
    }


}
