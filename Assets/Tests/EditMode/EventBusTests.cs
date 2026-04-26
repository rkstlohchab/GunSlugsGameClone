using GunSlugsClone.Core;
using NUnit.Framework;

namespace GunSlugsClone.Tests
{
    public class EventBusTests
    {
        private struct PingEvent { public int N; public PingEvent(int n) { N = n; } }

        [SetUp] public void Reset() => EventBus.ClearAll();

        [Test]
        public void Publish_ReachesAllSubscribers()
        {
            var a = 0; var b = 0;
            System.Action<PingEvent> ha = e => a += e.N;
            System.Action<PingEvent> hb = e => b += e.N;
            EventBus.Subscribe(ha);
            EventBus.Subscribe(hb);
            EventBus.Publish(new PingEvent(5));
            Assert.AreEqual(5, a);
            Assert.AreEqual(5, b);
        }

        [Test]
        public void Unsubscribe_StopsDelivery()
        {
            var hits = 0;
            System.Action<PingEvent> h = _ => hits++;
            EventBus.Subscribe(h);
            EventBus.Publish(new PingEvent(1));
            EventBus.Unsubscribe(h);
            EventBus.Publish(new PingEvent(1));
            Assert.AreEqual(1, hits);
        }

        [Test]
        public void PublishWithNoSubscribers_DoesNotThrow()
        {
            Assert.DoesNotThrow(() => EventBus.Publish(new PingEvent(1)));
        }

        [Test]
        public void SubscribingTwice_DeliversTwice()
        {
            var hits = 0;
            System.Action<PingEvent> h = _ => hits++;
            EventBus.Subscribe(h);
            EventBus.Subscribe(h);
            EventBus.Publish(new PingEvent(1));
            Assert.AreEqual(2, hits);
        }
    }
}
