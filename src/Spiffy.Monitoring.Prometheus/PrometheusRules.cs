using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Prometheus;

namespace Spiffy.Monitoring.Prometheus
{
    public class PrometheusRules
    {
        public static EventContextApi FromEventContext(string component, string operation)
        {
            return new EventContextApi(component, operation);
        }

        public class EventContextApi
        {
            readonly string _component;
            readonly string _operation;
            readonly List<string> _labels = new List<string>();
            Func<LogEvent, IDictionary<string, string>> _overrideValuesCallback;

            public EventContextApi(string component, string operation)
            {
                _component = component;
                _operation = operation;
            }

            public EventContextApi IncludeLabels(params string [] labelNames)
            {
                if (labelNames != null && labelNames.Any())
                {
                    _labels.AddRange(labelNames);
                }
                return this;
            }

            public EventContextApi OverrideValues(Func<LogEvent, IDictionary<string, string>> overrideValuesCallback)
            {
                _overrideValuesCallback = overrideValuesCallback;
                return this;
            }

            public EventContextApi ToCounter(string counterName, string description)
            {
                var rule = new CounterRule(_component, _operation, counterName, description, _labels.ToArray(), _overrideValuesCallback);
                CounterRules.GetOrAdd(rule.GetKey(), rule);
                return this;
            }
        }

        public static ConcurrentDictionary<string, CounterRule> CounterRules { get; } = new ConcurrentDictionary<string, CounterRule>();

        public static void Process(LogEvent logEvent)
        {
            var rule = FindRule(logEvent);
            if (rule != null)
            {
                try
                {
                    var properties = logEvent.Properties;
                    var overrides = rule.OverrideValues?.Invoke(logEvent);
                    if (overrides != null && overrides.Any())
                    {
                        foreach (var kvp in overrides)
                        {
                            properties[kvp.Key] = kvp.Value;
                        }
                    }

                    var counter = rule.Counter;
                    if (rule.LabelNames != null && rule.LabelNames.Any())
                    {
                        counter
                            .WithLabels(rule.LabelNames
                                .Select(label => properties
                                    .Single(p =>
                                        string.Compare(label, p.Key, StringComparison.OrdinalIgnoreCase) == 0)
                                    .Value).ToArray())
                            .Inc();
                    }
                    else
                    {
                        counter.Inc();
                    }
                }
                catch (Exception ex)
                {
                    using (var context = new EventContext("Prometheus", "Metrics"))
                    {
                        context.IncludeException(ex);
                    }
                }
            }
        }

        static CounterRule FindRule(LogEvent logEvent)
        {
            CounterRules.TryGetValue(
                logEvent.GetKey(),
                out var rule);
            return rule;
        }
    }

    public class CounterRule
    {
        public CounterRule(string component, string operation, string metricName, string description, string[] labelNames, Func<LogEvent, IDictionary<string, string>> overrideValues)
        {
            Component = component;
            Operation = operation;
            MetricName = metricName;
            Description = description;
            LabelNames = labelNames;
            OverrideValues = overrideValues;
            Counter = CreateCounter();
        }
        
        Counter CreateCounter()
        {
            var counter = Metrics.CreateCounter(
                MetricName, Description, new CounterConfiguration
                {
                    LabelNames = LabelNames
                });
            Counter = counter;
            return counter;
        }

        public string Component { get; }
        public string Operation { get; }

        public string [] LabelNames { get; }

        public Func<LogEvent, IDictionary<string, string>> OverrideValues { get; }

        public Counter Counter { get; private set; }

        string MetricName { get; }

        string Description { get; }
    }

    static class KeyExtensions
    {
        public static string GetKey(this CounterRule rule)
        {
            return $"{rule.Component}/{rule.Operation}";
        }

        public static string GetKey(this LogEvent logEvent)
        {
            return logEvent.PrivateData["MetricsKey"];
        }
    }
}