using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Analyzer
{
    class SyncDict
    {
        public static List<string> potentialrels = new List<string>();
        public static List<string> potentialacqs = new List<string>();
        static SyncDict()
        {
            // load from the file eventually
            potentialrels.Add("Call|System.Threading.Tasks.Dataflow.DataflowBlock::Post");
            potentialrels.Add("Call|System.Threading.Tasks.Dataflow.ActionBlock::Post");
            potentialrels.Add("Call|ExtensionMethods::ToEpoch");
            potentialrels.Add("Call|statsd.net_Tests.Infrastructure.MockTimeWindowService::GetTimeWindow-End");
            potentialrels.Add("Call|statsd.net.shared.Messages.StatsdMessageFactory::ParseMessage-End");
            potentialrels.Add("Call|System.Threading.Monitor::Exit");
            potentialrels.Add("Call|statsd.net.Framework.TimedLatencyPercentileAggregatorBlockFactory/c__DisplayClass0_1::b__3-End");

            potentialacqs.Add("Call|statsd.net.shared.Factories.MessageParserBlockFactory/c__DisplayClass0_0::b__0-Begin");
            potentialacqs.Add("Call|statsd.net.Framework.TimedCalendargramAggregatorBlockFactory/c__DisplayClass3_0::b__2-Begin");
            potentialacqs.Add("Call|statsd.net.Framework.TimedCalendargramAggregatorBlockFactory/c__DisplayClass3_0::b__0-Begin");
            potentialacqs.Add("Call|System.Threading.Thread::Sleep");
            potentialacqs.Add("Call|statsd.net.Framework.TimedCounterAggregatorBlockFactory/c__DisplayClass0_0::b__0-Begin");
            potentialacqs.Add("Call|statsd.net.Framework.TimedCounterAggregatorBlockFactory/c__DisplayClass0_0::b__2-Begin");
            potentialacqs.Add("Call|System.Threading.Tasks.Dataflow.DataflowBlock::Receive");
            potentialacqs.Add("Call|statsd.net.Framework.TimedGaugeAggregatorBlockFactory/c__DisplayClass0_0::b__2-Begin");
            potentialacqs.Add("Call|statsd.net.Framework.TimedGaugeAggregatorBlockFactory/c__DisplayClass0_0::b__0-Begin");
            potentialacqs.Add("Call|statsd.net.Framework.TimedLatencyAggregatorBlockFactory/c__DisplayClass0_0::b__0-Begin");
            potentialacqs.Add("Call|statsd.net.shared.Structures.DatapointBox::Add");
            potentialacqs.Add("Call|System.Threading.Monitor::Enter");
            potentialacqs.Add("Call|statsd.net.Framework.TimedLatencyPercentileAggregatorBlockFactory/c__DisplayClass0_0::b__0-Begin");


        }
    }
}
