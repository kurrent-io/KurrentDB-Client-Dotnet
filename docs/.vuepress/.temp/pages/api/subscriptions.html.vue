<template><div><h1 id="catch-up-subscriptions" tabindex="-1"><a class="header-anchor" href="#catch-up-subscriptions"><span>Catch-up Subscriptions</span></a></h1>
<p>Subscriptions allow you to subscribe to a stream and receive notifications about new events added to the stream.</p>
<p>You provide an event handler and an optional starting point to the subscription. The handler is called for each event from the starting point onward.</p>
<p>If events already exist, the handler will be called for each event one by one until it reaches the end of the stream. The server will then notify the handler whenever a new event appears.</p>
<div class="hint-container tip">
<p class="hint-container-title">Tips</p>
<p>Check the <RouteLink to="/api/getting-started.html">Getting Started</RouteLink> guide to learn how to configure and use the client SDK.</p>
</div>
<h2 id="subscribing-from-the-start" tabindex="-1"><a class="header-anchor" href="#subscribing-from-the-start"><span>Subscribing from the start</span></a></h2>
<p>If you need to process all the events in the store, including historical events, you'll need to subscribe from the beginning. You can either subscribe to receive events from a single stream or subscribe to <code v-pre>$all</code> if you need to process all events in the database.</p>
<h3 id="subscribing-to-a-stream" tabindex="-1"><a class="header-anchor" href="#subscribing-to-a-stream"><span>Subscribing to a stream</span></a></h3>
<p>The simplest stream subscription looks like the following :</p>
<p>@<a href="@grpc:subscribing-to-streams/Program.cs">code{subscribe-to-stream}</a></p>
<p>The provided handler will be called for every event in the stream.</p>
<p>When you subscribe to a stream with link events, for example the <code v-pre>$ce</code> category stream, you need to set <code v-pre>resolveLinkTos</code> to <code v-pre>true</code>. Read more about it <a href="#resolving-link-to-s">below</a>.</p>
<h3 id="subscribing-to-all" tabindex="-1"><a class="header-anchor" href="#subscribing-to-all"><span>Subscribing to <code v-pre>$all</code></span></a></h3>
<p>Subscribing to <code v-pre>$all</code> is similar to subscribing to a single stream. The handler will be called for every event appended after the starting position.</p>
<p>@<a href="@grpc:subscribing-to-streams/Program.cs">code{subscribe-to-all}</a></p>
<h2 id="subscribing-from-a-specific-position" tabindex="-1"><a class="header-anchor" href="#subscribing-from-a-specific-position"><span>Subscribing from a specific position</span></a></h2>
<p>The previous examples subscribed to the stream from the beginning. That subscription invoked the handler for every event in the stream before waiting for new events.</p>
<p>Both stream and $all subscriptions accept a starting position if you want to read from a specific point onward. If events already exist at the position you subscribe to, they will be read on the server side and sent to the subscription.</p>
<p>Once caught up, the server will push any new events received on the streams to the client. There is no difference between catching up and live on the client side.</p>
<div class="hint-container warning">
<p class="hint-container-title">Warning</p>
<p>The positions provided to the subscriptions are exclusive. You will only receive the next event after the subscribed position.</p>
</div>
<h3 id="subscribing-to-a-stream-1" tabindex="-1"><a class="header-anchor" href="#subscribing-to-a-stream-1"><span>Subscribing to a stream</span></a></h3>
<p>To subscribe to a stream from a specific position, you must provide a <em>stream position</em>. This can be <code v-pre>Start</code>, <code v-pre>End</code> or a <em>big int</em> (unsigned 64 bit integer) position.</p>
<p>The following subscribes to the stream <code v-pre>some-stream</code> at position <code v-pre>20</code>, this means that events <code v-pre>21</code> and onward will be handled:</p>
<p>@<a href="@grpc:subscribing-to-streams/Program.cs">code{subscribe-to-stream-from-position}</a></p>
<h3 id="subscribing-to-all-1" tabindex="-1"><a class="header-anchor" href="#subscribing-to-all-1"><span>Subscribing to $all</span></a></h3>
<p>Subscribing to the <code v-pre>$all</code> stream is similar to subscribing to a regular stream. The difference is how to specify the starting position. For the <code v-pre>$all</code> stream, provide a <code v-pre>Position</code> structure that consists of two big integers: the prepare and commit positions. Use <code v-pre>Start</code>, <code v-pre>End</code>, or create a <code v-pre>Position</code> from specific commit and prepare values.</p>
<p>The corresponding <code v-pre>$all</code> subscription will subscribe from the event after the one at commit position <code v-pre>1056</code> and prepare position <code v-pre>1056</code>.</p>
<p>Please note that this position will need to be a legitimate position in <code v-pre>$all</code>.</p>
<p>@<a href="@grpc:subscribing-to-streams/Program.cs">code{subscribe-to-all-from-position}</a></p>
<h2 id="subscribing-to-a-stream-for-live-updates" tabindex="-1"><a class="header-anchor" href="#subscribing-to-a-stream-for-live-updates"><span>Subscribing to a stream for live updates</span></a></h2>
<p>You can subscribe to a stream to get live updates by subscribing to the end of the stream:</p>
<p>@<a href="@grpc:subscribing-to-streams/Program.cs">code{subscribe-to-stream-live}</a></p>
<p>And the same works with <code v-pre>$all</code> :</p>
<p>@<a href="@grpc:subscribing-to-streams/Program.cs">code{subscribe-to-all-live}</a></p>
<p>This will not read through the history of the stream but will notify the handler when a new event appears in the respective stream.</p>
<p>Keep in mind that when you subscribe to a stream from a specific position, as described <a href="#subscribing-from-a-specific-position">above</a>, you will also get live updates after your subscription catches up (processes all the historical events).</p>
<h2 id="resolving-link-to-events" tabindex="-1"><a class="header-anchor" href="#resolving-link-to-events"><span>Resolving link-to events</span></a></h2>
<p>Link-to events point to events in other streams in KurrentDB. These are generally created by projections such as the <code v-pre>$by_event_type</code> projection which links events of the same event type into the same stream. This makes it easier to look up all events of a specific type.</p>
<div class="hint-container tip">
<p class="hint-container-title">Tips</p>
<p><RouteLink to="/api/subscriptions.html#server-side-filtering">Filtered subscriptions</RouteLink> make it easier and faster to subscribe to all events of a specific type or matching a prefix.</p>
</div>
<p>When reading a stream you can specify whether to resolve link-to's. By default, link-to events are not resolved. You can change this behaviour by setting the <code v-pre>resolveLinkTos</code> parameter to <code v-pre>true</code>:</p>
<p>@<a href="@grpc:subscribing-to-streams/Program.cs">code{subscribe-to-stream-resolving-linktos}</a></p>
<h2 id="dropped-subscriptions" tabindex="-1"><a class="header-anchor" href="#dropped-subscriptions"><span>Dropped subscriptions</span></a></h2>
<p>When a subscription stops or experiences an error, it will be dropped. The subscription provides a <code v-pre>subscriptionDropped</code> callback, which will get called when the subscription breaks.</p>
<p>The <code v-pre>subscriptionDropped</code> callback allows you to inspect the reason why the subscription dropped, as well as any exceptions that occurred.</p>
<p>The possible reasons for a subscription to drop are:</p>
<table>
<thead>
<tr>
<th style="text-align:left">Reason</th>
<th style="text-align:left">Why it might happen</th>
</tr>
</thead>
<tbody>
<tr>
<td style="text-align:left"><code v-pre>Disposed</code></td>
<td style="text-align:left">The client canceled or disposed of the subscription.</td>
</tr>
<tr>
<td style="text-align:left"><code v-pre>SubscriberError</code></td>
<td style="text-align:left">An error occurred while handling an event in the subscription handler.</td>
</tr>
<tr>
<td style="text-align:left"><code v-pre>ServerError</code></td>
<td style="text-align:left">An error occurred on the server, and the server closed the subscription. Check the server logs for more information.</td>
</tr>
</tbody>
</table>
<p>Bear in mind that a subscription can also drop because it is slow. The server tried to push all the live events to the subscription when it is in the live processing mode. If the subscription gets the reading buffer overflow and won't be able to acknowledge the buffer, it will break.</p>
<h3 id="handling-subscription-drops" tabindex="-1"><a class="header-anchor" href="#handling-subscription-drops"><span>Handling subscription drops</span></a></h3>
<p>An application, which hosts the subscription, can go offline for some time for different reasons. It could be a crash, infrastructure failure, or a new version deployment. As you rarely would want to reprocess all the events again, you'd need to store the current position of the subscription somewhere, and then use it to restore the subscription from the point where it dropped off:</p>
<p>@<a href="@grpc:subscribing-to-streams/Program.cs">code{subscribe-to-stream-subscription-dropped}</a></p>
<p>When subscribed to <code v-pre>$all</code> you want to keep the event's position in the <code v-pre>$all</code> stream. As mentioned previously, the <code v-pre>$all</code> stream position consists of two big integers (prepare and commit positions), not one:</p>
<p>@<a href="@grpc:subscribing-to-streams/Program.cs">code{subscribe-to-all-subscription-dropped}</a></p>
<h2 id="user-credentials" tabindex="-1"><a class="header-anchor" href="#user-credentials"><span>User credentials</span></a></h2>
<p>The user creating a subscription must have read access to the stream it's subscribing to, and only admin users may subscribe to <code v-pre>$all</code> or create filtered subscriptions.</p>
<p>The code below shows how you can provide user credentials for a subscription. When you specify subscription credentials explicitly, it will override the default credentials set for the client. If you don't specify any credentials, the client will use the credentials specified for the client, if you specified those.</p>
<p>@<a href="@grpc:subscribing-to-streams/Program.cs">code{overriding-user-credentials}</a></p>
<h2 id="server-side-filtering" tabindex="-1"><a class="header-anchor" href="#server-side-filtering"><span>Server-side filtering</span></a></h2>
<p>KurrentDB allows you to filter the events whilst subscribing to the <code v-pre>$all</code> stream to only receive the events you care about.</p>
<p>You can filter by event type or stream name using a regular expression or a prefix. Server-side filtering is currently only available on the <code v-pre>$all</code> stream.</p>
<div class="hint-container tip">
<p class="hint-container-title">Tips</p>
<p>Server-side filtering was introduced as a simpler alternative to projections. You should consider filtering before creating a projection to include the events you care about.</p>
</div>
<p>A simple stream prefix filter looks like this:</p>
<p>@<a href="@grpc:subscribing-to-streams/Program.cs">code{stream-prefix-filtered-subscription}</a></p>
<p>The filtering API is described more in-depth in the <RouteLink to="/api/subscriptions.html#server-side-filtering">filtering section</RouteLink>.</p>
<h3 id="filtering-out-system-events" tabindex="-1"><a class="header-anchor" href="#filtering-out-system-events"><span>Filtering out system events</span></a></h3>
<p>There are events in KurrentDB called system events. These are prefixed with a <code v-pre>$</code> and under most circumstances you won't care about these. They can be filtered out by passing in a <code v-pre>SubscriptionFilterOptions</code> when subscribing to the <code v-pre>$all</code> stream.</p>
<p>@<a href="@grpc:server-side-filtering/Program.cs">code{exclude-system}</a></p>
<div class="hint-container tip">
<p class="hint-container-title">Tips</p>
<p><code v-pre>$stats</code> events are no longer stored in KurrentDB by default so there won't be as many <code v-pre>$</code> events as before.</p>
</div>
<h3 id="filtering-by-event-type" tabindex="-1"><a class="header-anchor" href="#filtering-by-event-type"><span>Filtering by event type</span></a></h3>
<p>If you only want to subscribe to events of a given type, there are two options. You can either use a regular expression or a prefix.</p>
<h4 id="filtering-by-prefix" tabindex="-1"><a class="header-anchor" href="#filtering-by-prefix"><span>Filtering by prefix</span></a></h4>
<p>If you want to filter by prefix, pass in a <code v-pre>SubscriptionFilterOptions</code> to the subscription with an <code v-pre>EventTypeFilter.Prefix</code>.</p>
<p>@<a href="@grpc:server-side-filtering/Program.cs">code{event-type-prefix}</a></p>
<p>This will only subscribe to events with a type that begin with <code v-pre>customer-</code>.</p>
<h4 id="filtering-by-regular-expression" tabindex="-1"><a class="header-anchor" href="#filtering-by-regular-expression"><span>Filtering by regular expression</span></a></h4>
<p>It might be advantageous to provide a regular expression when you want to subscribe to multiple event types.</p>
<p>@<a href="@grpc:server-side-filtering/Program.cs">code{event-type-regex}</a></p>
<p>This will subscribe to any event that begins with <code v-pre>user</code> or <code v-pre>company</code>.</p>
<h3 id="filtering-by-stream-name" tabindex="-1"><a class="header-anchor" href="#filtering-by-stream-name"><span>Filtering by stream name</span></a></h3>
<p>To subscribe to a stream by name, choose either a regular expression or a prefix.</p>
<h4 id="filtering-by-prefix-1" tabindex="-1"><a class="header-anchor" href="#filtering-by-prefix-1"><span>Filtering by prefix</span></a></h4>
<p>If you want to filter by prefix, pass in a <code v-pre>SubscriptionFilterOptions</code> to the subscription with an <code v-pre>StreamFilter.Prefix</code>.</p>
<p>@<a href="@grpc:server-side-filtering/Program.cs">code{stream-prefix}</a></p>
<p>This will only subscribe to all streams with a name that begins with <code v-pre>user-</code>.</p>
<h4 id="filtering-by-regular-expression-1" tabindex="-1"><a class="header-anchor" href="#filtering-by-regular-expression-1"><span>Filtering by regular expression</span></a></h4>
<p>To subscribe to multiple streams, use a regular expression.</p>
<p>@<a href="@grpc:server-side-filtering/Program.cs">code{stream-regex}</a></p>
<p>This will subscribe to any stream with a name that begins with <code v-pre>account</code> or <code v-pre>savings</code>.</p>
<h2 id="checkpointing" tabindex="-1"><a class="header-anchor" href="#checkpointing"><span>Checkpointing</span></a></h2>
<p>When a catch-up subscription is used to process an <code v-pre>$all</code> stream containing many events, the last thing you want is for your application to crash midway, forcing you to restart from the beginning.</p>
<h3 id="what-is-a-checkpoint" tabindex="-1"><a class="header-anchor" href="#what-is-a-checkpoint"><span>What is a checkpoint?</span></a></h3>
<p>A checkpoint is the position of an event in the <code v-pre>$all</code> stream to which your application has processed. By saving this position to a persistent store (e.g., a database), it allows your catch-up subscription to:</p>
<ul>
<li>Recover from crashes by reading the checkpoint and resuming from that position</li>
<li>Avoid reprocessing all events from the start</li>
</ul>
<p>To create a checkpoint, store the event's commit or prepare position.</p>
<div class="hint-container warning">
<p class="hint-container-title">Warning</p>
<p>If your database contains events created by the legacy TCP client using the <a href="https://docs.kurrent.io/clients/tcp/dotnet/21.2/appending.html#transactions" target="_blank" rel="noopener noreferrer">transaction feature</a>, you should store both the commit and prepare positions together as your checkpoint.</p>
</div>
<h3 id="updating-checkpoints-at-regular-intervals" tabindex="-1"><a class="header-anchor" href="#updating-checkpoints-at-regular-intervals"><span>Updating checkpoints at regular intervals</span></a></h3>
<p>The client SDK provides a way to notify your application after processing a configurable number of events. This allows you to periodically save a checkpoint at regular intervals.</p>
<p>@<a href="@grpc:server-side-filtering/Program.cs">code{checkpoint}</a></p>
<p>By default, the checkpoint notification is sent after every 32 non-system events processed from $all.</p>
<h3 id="configuring-the-checkpoint-interval" tabindex="-1"><a class="header-anchor" href="#configuring-the-checkpoint-interval"><span>Configuring the checkpoint interval</span></a></h3>
<p>You can adjust the checkpoint interval to change how often the client is notified.</p>
<p>@<a href="@grpc:server-side-filtering/Program.cs">code{checkpoint-with-interval}</a></p>
<p>By configuring this parameter, you can balance between reducing checkpoint overhead and ensuring quick recovery in case of a failure.</p>
<div class="hint-container info">
<p class="hint-container-title">Info</p>
<p>The checkpoint interval parameter configures the database to notify the client after <code v-pre>n</code> * 32 number of events where <code v-pre>n</code> is defined by the parameter.</p>
<p>For example:</p>
<ul>
<li>If <code v-pre>n</code> = 1, a checkpoint notification is sent every 32 events.</li>
<li>If <code v-pre>n</code> = 2, the notification is sent every 64 events.</li>
<li>If <code v-pre>n</code> = 3, it is sent every 96 events, and so on.</li>
</ul>
</div>
</div></template>


