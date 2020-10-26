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

            public EventContextApi(string component, string operation)
            {
                _component = component;
                _operation = operation;
            }

            public EventContextApi ToCounter(string counterName, string description)
            {
                var rule = new CounterRule(_component, _operation, counterName, description, _labels.ToArray());
                CounterRules.GetOrAdd(rule.GetKey(), rule);
                return this;
            }

            public EventContextApi IncludeLabels(params string [] labelNames)
            {
                _labels.AddRange(labelNames);
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
                    rule.Counter
                        .WithLabels(rule.AdditionalLabels
                            .Select(label => logEvent.Properties
                                .Single(p =>
                                    string.Compare(label, p.Key, StringComparison.OrdinalIgnoreCase) == 0)
                                .Value).ToArray())
                        .Inc();
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
        public CounterRule(string component, string operation, string metricName, string description, string[] additionalLabels)
        {
            Component = component;
            Operation = operation;
            MetricName = metricName;
            Description = description;
            AdditionalLabels = additionalLabels;
            Counter = CreateCounter();
        }
        
        Counter CreateCounter()
        {
            var counter = Metrics.CreateCounter(
                MetricName, Description, new CounterConfiguration
                {
                    LabelNames = AdditionalLabels
                });
            Counter = counter;
            return counter;
        }

        public string Component { get; }
        public string Operation { get; }

        /// <summary>
        /// Get/set the metric name to increment.  NOTE: this should follow a
        /// `gyi_{service-name}_{domain-function}_total convention` e.g.
        /// `gyi_purchase_checkout_total`
        /// </summary>
        public string MetricName { get; }

        public string Description { get; }

        /// <summary>
        /// Get/set additional labels to include in the metric.
        /// </summary>
        public string [] AdditionalLabels { get; }

        public Counter Counter { get; private set; }
    }

    static class KeyExtensions
    {
        public static string GetKey(this CounterRule rule)
        {
            return GetCompositeKey(rule.Component, rule.Operation);
        }

        public static string GetKey(this LogEvent logEvent)
        {
            return GetCompositeKey(
                logEvent.Properties["Component"],
                logEvent.Properties["Operation"]);
        }

        static string GetCompositeKey(string part1, string part2)
        {
            return $"{part1}/{part2}";
        }
    }
}