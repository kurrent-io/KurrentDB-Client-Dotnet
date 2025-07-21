<template><div><h1 id="appending-events" tabindex="-1"><a class="header-anchor" href="#appending-events"><span>Appending Events</span></a></h1>
<p>When you start working with KurrentDB, it is empty. The first meaningful operation is to add one or more events to the database using one of the available client SDKs.</p>
<div class="hint-container tip">
<p class="hint-container-title">Tips</p>
<p>Check the <RouteLink to="/api/getting-started.html">Getting Started</RouteLink> guide to learn how to configure and use the client SDK.</p>
</div>
<h2 id="append-your-first-event" tabindex="-1"><a class="header-anchor" href="#append-your-first-event"><span>Append your first event</span></a></h2>
<p>The simplest way to append an event to KurrentDB is to create an <code v-pre>EventData</code> object and call <code v-pre>AppendToStream</code> method.</p>
<p>@<a href="@grpc:appending-events/Program.cs">code{append-to-stream}</a></p>
<p><code v-pre>AppendToStream</code> takes a collection of <code v-pre>EventData</code>, which allows you to save more than one event in a single batch.</p>
<p>Outside the example above, other options exist for dealing with different scenarios.</p>
<div class="hint-container tip">
<p class="hint-container-title">Tips</p>
<p>If you are new to Event Sourcing, please study the <a href="#handling-concurrency">Handling concurrency</a> section below.</p>
</div>
<h2 id="working-with-eventdata" tabindex="-1"><a class="header-anchor" href="#working-with-eventdata"><span>Working with EventData</span></a></h2>
<p>Events appended to KurrentDB must be wrapped in an <code v-pre>EventData</code> object. This allows you to specify the event's content, the type of event, and whether it's in JSON format. In its simplest form, you need three arguments:  <strong>eventId</strong>, <strong>type</strong>, and <strong>data</strong>.</p>
<h3 id="eventid" tabindex="-1"><a class="header-anchor" href="#eventid"><span>eventId</span></a></h3>
<p>This takes the format of a <code v-pre>Uuid</code> and is used to uniquely identify the event you are trying to append. If two events with the same <code v-pre>Uuid</code> are appended to the same stream in quick succession, KurrentDB will only append one of the events to the stream.</p>
<p>For example, the following code will only append a single event:</p>
<p>@<a href="@grpc:appending-events/Program.cs">code{append-duplicate-event}</a></p>
<figure><img src="@source/images/duplicate-event.png" alt="Duplicate Event" tabindex="0" loading="lazy"><figcaption>Duplicate Event</figcaption></figure>
<h3 id="type" tabindex="-1"><a class="header-anchor" href="#type"><span>type</span></a></h3>
<p>Each event should be supplied with an event type. This unique string is used to identify the type of event you are saving.</p>
<p>It is common to see the explicit event code type name used as the type as it makes serialising and de-serialising of the event easy. However, we recommend against this as it couples the storage to the type and will make it more difficult if you need to version the event at a later date.</p>
<h3 id="data" tabindex="-1"><a class="header-anchor" href="#data"><span>data</span></a></h3>
<p>Representation of your event data. It is recommended that you store your events as JSON objects.  This allows you to take advantage of all of KurrentDB's functionality, such as projections. That said, you can save events using whatever format suits your workflow. Eventually, the data will be stored as encoded bytes.</p>
<h3 id="metadata" tabindex="-1"><a class="header-anchor" href="#metadata"><span>metadata</span></a></h3>
<p>Storing additional information alongside your event that is part of the event itself is standard practice. This can be correlation IDs, timestamps, access information, etc. KurrentDB allows you to store a separate byte array containing this information to keep it separate.</p>
<h3 id="isjson" tabindex="-1"><a class="header-anchor" href="#isjson"><span>isJson</span></a></h3>
<p>Simple boolean field to tell KurrentDB if the event is stored as json, true by default.</p>
<h2 id="handling-concurrency" tabindex="-1"><a class="header-anchor" href="#handling-concurrency"><span>Handling concurrency</span></a></h2>
<p>When appending events to a stream, you can supply a <em>stream state</em> or <em>stream revision</em>. Your client uses this to inform KurrentDB of the state or version you expect the stream to be in when appending an event. If the stream isn't in that state, an exception will be thrown.</p>
<p>For example, if you try to append the same record twice, expecting both times that the stream doesn't exist, you will get an exception on the second:</p>
<p>@<a href="@grpc:appending-events/Program.cs">code{append-with-no-stream}</a></p>
<p>There are three available stream states:</p>
<ul>
<li><code v-pre>Any</code></li>
<li><code v-pre>NoStream</code></li>
<li><code v-pre>StreamExists</code></li>
</ul>
<p>This check can be used to implement optimistic concurrency. When retrieving a stream from KurrentDB, note the current version number. When you save it back, you can determine if somebody else has modified the record in the meantime.</p>
<p>@<a href="@grpc:appending-events/Program.cs">code{append-with-concurrency-check}</a></p>
<!-- ## Options TODO -->
<h2 id="user-credentials" tabindex="-1"><a class="header-anchor" href="#user-credentials"><span>User credentials</span></a></h2>
<p>You can provide user credentials to append the data as follows. This will override the default credentials set on the connection.</p>
<p>@<a href="@grpc:appending-events/Program.cs">code{overriding-user-credentials}</a></p>
</div></template>


