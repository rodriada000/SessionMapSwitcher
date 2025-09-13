using System;

namespace SessionMapSwitcherCore.Classes
{
    public static class NumericExtensionMethods
    {
        public static double MapRange(this double input_value, double input_max, double output_max)
        {
            return input_value.MapRange(0, input_max, 0, output_max);
        }

        public static double MapRange(this double input_value, double input_start, double input_end, double output_start, double output_end)
        {
            // Normalize the input value to the range [0, 1]
            double normalized_value = (input_value - input_start) / (input_end - input_start);

            // Clamp the normalized value to [0, 1] if needed
            normalized_value = Math.Clamp(normalized_value, 0, 1);

            // Scale and shift the normalized value to the output range
            return output_start + normalized_value * (output_end - output_start);
        }

        public static double MapRange(this int input_value, double input_max, double output_max)
        {
            return input_value.MapRange(0, input_max, 0, output_max);
        }


        public static double MapRange(this int input_value, double input_start, double input_end, double output_start, double output_end)
        {
            // Normalize the input value to the range [0, 1]
            double normalized_value = (input_value - input_start) / (input_end - input_start);

            // Clamp the normalized value to [0, 1] if needed
            normalized_value = Math.Clamp(normalized_value, 0, 1);

            // Scale and shift the normalized value to the output range
            return output_start + normalized_value * (output_end - output_start);
        }
    }
}
