using System;
using System.Text;
using Unity.Netcode;
using UnityEngine;

namespace Multiplayer
{
    class Calibrator
    {
        int iterations = 0;
        const int MAX_ITERATIONS = 20;

        readonly Vector3[] sums;
        readonly Vector3[] averages;

        public Calibrator(int num)
        {
            sums = new Vector3[num];
            averages = new Vector3[num];
        }

        bool calibrating = true;

        public void StartCalibration()
        {
            calibrating = true;
        }

        public void Calibrate(Vector3[] values, Action<Vector3[]> OnCalibrated)
        {
            if (!calibrating)
            {
                return;
            }

            for (int i = 0; i < sums.Length; i++)
            {
                sums[i] += values[i];
            }

            iterations++;

            if (iterations >= MAX_ITERATIONS)
            {
                for (int i = 0; i < averages.Length; i++)
                {
                    averages[i] = sums[i] / MAX_ITERATIONS;
                }

                OnCalibrated(averages);

                calibrating = false;
                iterations = 0;

                for (int i = 0; i < sums.Length; i++)
                {
                    sums[i] = Vector3.zero;
                }
            }
        }
    }

    public class Utils : MonoBehaviour
    {
        public static LogLevel s_LogLevel = LogLevel.Developer;

        public static void LogError(string message) => Log(message, 2);
        public static void LogWarning(string message) => Log(message, 1);
        public static void Log(string message, int logLevel = 0, string prefix = "XRMultiplayer")
        {
            if (s_LogLevel == LogLevel.Nothing) return;
            StringBuilder sb = new($"<color=#33FF64>[{prefix}]</color>");
            sb.Append(message);

            switch (logLevel)
            {
                case 0:
                    if (s_LogLevel == 0)
                        Debug.Log(sb);
                    break;
                case 1:
                    if ((int)s_LogLevel < 2)
                        Debug.LogWarning(sb);
                    break;
                case 2:
                    Debug.LogError(sb);
                    break;
            }
        }

        public static string GetOrdinal(int num)
        {
            if (num <= 0) return num.ToString();

            switch (num % 100)
            {
                case 11:
                case 12:
                case 13:
                    return "th";
            }

            switch (num % 10)
            {
                case 1:
                    return "st";
                case 2:
                    return "nd";
                case 3:
                    return "rd";
                default:
                    return "th";
            }
        }

        public static string GetTimeFormatted(float time)
        {
            TimeSpan timeSpan = TimeSpan.FromSeconds(time);
            return timeSpan.ToString("mm':'ss'.'ff");
        }

        public static int RealMod(int a, int b)
        {
            return (a % b + b) % b;
        }

        public static float GetPercentOfValueBetweenTwoValues(float min, float max, float input)
        {
            input = Mathf.Clamp(input, min, max);

            return (input - min) / (max - min);
        }

        public static bool IsPlayerLookingTowards(Transform playerCamera, Transform target, float dotProductThreshold = 0.8f)
        {
            Vector3 directionToTarget = (target.position - playerCamera.position).normalized;
            float dotProduct = Vector3.Dot(playerCamera.forward, directionToTarget);

            return dotProduct >= dotProductThreshold;
        }
    }
}
