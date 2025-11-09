using System;
using System.Collections.Generic;
using System.Linq;
using NAudio.Wave;

namespace CrossfadER;

public class CrossfadeHandler
{
    public static void Execute(string crossfadeFilePath,
        string crossfadeTimeStr,
        CrossfadeMode crossfadeMode,
        string outputFilePath)
    {
        try
        {
            HandleCrossfadeData crossfadeData = new HandleCrossfadeData();
            float crossfadeTime = crossfadeData.GetCrossfadeTime(crossfadeTimeStr);
            using (var audioFile = new MediaFoundationReader(crossfadeFilePath))
            {
                Console.WriteLine(CheckAudioDuration(audioFile, crossfadeTime));
                if (!CheckAudioDuration(audioFile, crossfadeTime))
                {
                    MainWindow.UpdateNote("Crossfade time needs to be shorter than audio duration", MainWindow.note);
                    return;
                }
                
                int sampleRate = audioFile.WaveFormat.SampleRate;
                int channels = audioFile.WaveFormat.Channels;
                
                var sampler = audioFile.ToSampleProvider(); // converts to ISampleProvider (float)
                
                List<float> samplesList = new List<float>();
                float[] buffer = new float[1024];
                int read;
    
                while ((read = sampler.Read(buffer, 0, buffer.Length)) > 0)
                {
                    samplesList.AddRange(buffer.Take(read));
                }

                float[] samples = samplesList.ToArray();

                float[] swappedSamples = CutAndSwapAudio(samples);
                float[] crossfadedSamples = CrossfadeAudio(swappedSamples, sampleRate, channels, crossfadeTime, crossfadeMode);
                
                var outputFormat = WaveFormat.CreateIeeeFloatWaveFormat(
                    sampler.WaveFormat.SampleRate,
                    sampler.WaveFormat.Channels);
                
                using (var fileWriter = new WaveFileWriter(outputFilePath, outputFormat))
                {
                    fileWriter.WriteSamples(crossfadedSamples, 0, crossfadedSamples.Length);
                    
                    MainWindow.UpdateProgressBar(100f, "DONE", MainWindow.progressBar, MainWindow.statusTextBlock);
                }
            }
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
        }
    }

    private static float[] CutAndSwapAudio(float[] audioSampleData)
    {
        int midPoint =  audioSampleData.Length / 2;
        float[] result = new float[audioSampleData.Length];

        Array.Copy(audioSampleData, midPoint, result, 0, audioSampleData.Length - midPoint);
        Array.Copy(audioSampleData, 0, result, audioSampleData.Length - midPoint, midPoint);
        
        return result;
    }

    private static float[] CrossfadeAudio(float[] audioSampleData, int sampleRate, int channels, float crossfadeTime, CrossfadeMode crossfadeMode)
    {
        int crossfadeSamples = (int) (sampleRate * crossfadeTime);
        
        float[] result = new float[audioSampleData.Length - crossfadeSamples / 2];
        
        int start = FindWaveAtZero(audioSampleData, audioSampleData.Length / 2, 1000);
        
        Array.Copy(audioSampleData, 0, result, 0, start);
        Array.Copy(audioSampleData, start + crossfadeSamples, result, start + crossfadeSamples, result.Length - start - crossfadeSamples);

        for (int i = 0; i < crossfadeSamples; i++)
        {
            float progress = (float) i / result.Length;
            MainWindow.UpdateProgressBar(progress, "Processing...", MainWindow.progressBar, MainWindow.statusTextBlock);
            
            int indexLeft = start + i;
            int indexRight = indexLeft + crossfadeSamples / 2;
            
            float temp = (float) i / crossfadeSamples;

            CrossfadeOutput coefficients = new CrossfadeOutput();
            
            switch (crossfadeMode)
            {
                case CrossfadeMode.LINEAR:
                    coefficients = LinearCrossfade(temp);
                    break;
                case CrossfadeMode.LOG:
                    coefficients = LogCrossfade(temp);
                    break;
                case CrossfadeMode.SINE:
                    coefficients = SineCrossfade(temp);
                    break;
                default:
                    break;
            }
            
            for (int j = 0; j < channels; j++)
            {
                int targetIndex = indexLeft + j;
                result[targetIndex + j] = coefficients.CoefficientA * audioSampleData[indexLeft + j] + coefficients.CoefficientB * audioSampleData[indexRight + j];
            }
        }
        
        return result;
    }

    private static CrossfadeOutput LinearCrossfade(float temp)
    {
        CrossfadeOutput coefficients = new CrossfadeOutput
        {
            CoefficientA = 1 - temp,
            CoefficientB = temp
        };

        return coefficients;
    }

    private static CrossfadeOutput SineCrossfade(float temp)
    {
        CrossfadeOutput coefficients = new CrossfadeOutput
        {
            CoefficientA = (float)Math.Cos(temp * Math.PI / 2),
            CoefficientB = (float)Math.Sin(temp * Math.PI / 2)
        };

        return coefficients;
    }
    
    private static CrossfadeOutput LogCrossfade(float temp)
    {
        CrossfadeOutput coefficients = new CrossfadeOutput
        {
            CoefficientA = (float)Math.Pow(temp, 0.3),
            CoefficientB = (float)Math.Pow(1 - temp, 0.3)
        };

        return coefficients;
    }

    private static int FindWaveAtZero(float[] audioSampleData, int startIndex, int range)
    {
        int bestIndex = startIndex;
        float minValue = Math.Abs(audioSampleData[startIndex]);

        for (int i = Math.Max(0, startIndex - range); i < Math.Min(audioSampleData.Length, startIndex + range); i++)
        {
            float absVal = Math.Abs(audioSampleData[i]);
            if (absVal < minValue)
            {
                minValue = absVal;
                bestIndex = i;
            }
        }
        
        return bestIndex;
    }

    private static bool CheckAudioDuration(MediaFoundationReader reader, float crossfadeTime)
    {
        TimeSpan timespan = reader.TotalTime;
        
        Console.WriteLine(timespan.TotalSeconds);
        Console.WriteLine(crossfadeTime);
        
        if (timespan.TotalSeconds < crossfadeTime)
        {
            return false;
        }

        return true;
    }
}

public enum CrossfadeMode
{
    LINEAR,
    SINE,
    LOG
}

struct CrossfadeOutput
{
    public float CoefficientA;
    public float CoefficientB;
}