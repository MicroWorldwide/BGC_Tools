﻿using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using BGC.Mathematics;

namespace BGC.Audio.AnalyticStreams
{
    public class AnalyticStreamAdder : AnalyticFilter
    {
        private readonly List<IAnalyticStream> streams = new List<IAnalyticStream>();
        public override IEnumerable<IAnalyticStream> InternalStreams => streams;

        private int sampleCount = 0;
        public override int Samples => sampleCount;

        private double samplingRate;
        public override double SamplingRate => samplingRate;

        private const int BUFFER_SIZE = 512;
        private readonly Complex64[] buffer = new Complex64[BUFFER_SIZE];

        public AnalyticStreamAdder()
        {
            UpdateStats();
        }

        public AnalyticStreamAdder(params IAnalyticStream[] streams)
        {
            AddStreams(streams);
        }

        public AnalyticStreamAdder(IEnumerable<IAnalyticStream> streams)
        {
            AddStreams(streams);
        }

        public void AddStream(IAnalyticStream stream)
        {
            streams.Add(stream);
            UpdateStats();
        }

        public void AddStreams(IEnumerable<IAnalyticStream> streams)
        {
            this.streams.AddRange(streams);
            UpdateStats();
        }

        public bool RemoveStream(IAnalyticStream stream)
        {
            bool success = streams.Remove(stream);
            UpdateStats();
            return success;
        }

        public override void Reset() => streams.ForEach(x => x.Reset());

        public override int Read(Complex64[] data, int offset, int count)
        {
            int minRemainingSamples = count;

            Array.Clear(data, offset, count);

            foreach (IAnalyticStream stream in streams)
            {
                int streamRemainingSamples = count;
                int streamOffset = offset;

                while (streamRemainingSamples > 0)
                {
                    int maxRead = Math.Min(BUFFER_SIZE, streamRemainingSamples);
                    int streamReadSamples = stream.Read(buffer, 0, maxRead);

                    if (streamReadSamples == 0)
                    {
                        //Done with this stream
                        break;
                    }

                    for (int i = 0; i < streamReadSamples; i++)
                    {
                        data[streamOffset + i] += buffer[i];
                    }

                    streamOffset += streamReadSamples;
                    streamRemainingSamples -= streamReadSamples;
                }

                minRemainingSamples = Math.Min(minRemainingSamples, streamRemainingSamples);
            }

            return count - minRemainingSamples;
        }

        public override void Seek(int position) => streams.ForEach(x => x.Seek(position));

        private void UpdateStats()
        {
            if (streams.Count > 0)
            {
                IEnumerable<double> samplingRates = streams.Select(x => x.SamplingRate);
                samplingRate = samplingRates.Max();

                if (samplingRate != samplingRates.Min())
                {
                    throw new StreamCompositionException("AnalyticStreamAdder requires all streams have the same samplingRate.");
                }

                sampleCount = streams.Select(x => x.Samples).Max();
                channelRMS = double.NaN;
            }
            else
            {
                sampleCount = 0;
                samplingRate = 44100.0;
                channelRMS = double.NaN;
            }
        }

        private double channelRMS = double.NaN;
        //RMS for each channel will be the sum of the constituent RMS's
        public override double GetRMS()
        {
            if (double.IsNaN(channelRMS))
            {
                channelRMS = streams.Select(x => { double rms = x.GetRMS(); return rms * rms; }).Sum();
                channelRMS = Math.Sqrt(channelRMS);
            }

            return channelRMS;
        }

        private bool presentationConstraintsChecked = false;
        private PresentationConstraints presentationConstraints = null;
        public override PresentationConstraints GetPresentationConstraints()
        {
            if (!presentationConstraintsChecked)
            {
                presentationConstraintsChecked = true;
                presentationConstraints = PresentationConstraints.ExtractSetConstraints(streams);
            }

            return presentationConstraints;
        }

        public override void Dispose()
        {
            foreach (var stream in streams)
            {
                stream.Dispose();
            }
        }
    }
}
