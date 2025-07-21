<template><div><h1 id="reading-events" tabindex="-1"><a class="header-anchor" href="#reading-events"><span>Reading Events</span></a></h1>
<p>There are two options for reading events from KurrentDB. You can either:
1. Read from an individual stream, or
2. Read from the <code v-pre>$all</code> stream, which will return all events in the store.</p>
<p>Each event in KurrentDB belongs to an individual stream. When reading events, pick the name of the stream from which you want to read the events and choose whether to read the stream forwards or backwards.</p>
<p>All events have a <code v-pre>StreamPosition</code> and a <code v-pre>Position</code>.  <code v-pre>StreamPosition</code> is a <em>big int</em> (unsigned 64-bit integer) and represents the place of the event in the stream. <code v-pre>Position</code> is the event's logical position, and is represented by <code v-pre>CommitPosition</code> and a <code v-pre>PreparePosition</code>. Note that when reading events you will supply a different &quot;position&quot; depending on whether you are reading from an individual stream or the <code v-pre>$all</code> stream.</p>
<div class="hint-container tip">
<p class="hint-container-title">Tips</p>
<p>Check <RouteLink to="/api/getting-started.html#required-packages">connecting to KurrentDB instructions</RouteLink> to learn how to configure and use the client SDK.</p>
</div>
<h2 id="reading-from-a-stream" tabindex="-1"><a class="header-anchor" href="#reading-from-a-stream"><span>Reading from a stream</span></a></h2>
<p>You can read all the events or a sample of the events from individual streams, starting from any position in the stream, and can read either forward or backward. It is only possible to read events from a single stream at a time. You can read events from the global event log, which spans across streams. Learn more about this process in the <a href="#reading-from-the-all-stream">Read from <code v-pre>$all</code></a> section below.</p>
<h3 id="reading-forwards" tabindex="-1"><a class="header-anchor" href="#reading-forwards"><span>Reading forwards</span></a></h3>
<p>The simplest way to read a stream forwards is to supply a stream name, read direction, and revision from which to start. The revision can either be a <em>stream position</em> <code v-pre>Start</code> or a <em>big int</em> (unsigned 64-bit integer):</p>
<p>@<a href="@grpc:reading-events/Program.cs">code{read-from-stream}</a></p>
<p>This will return an enumerable that can be iterated on:</p>
<p>@<a href="@grpc:reading-events/Program.cs">code{iterate-stream}</a></p>
<p>There are a number of additional arguments you can provide when reading a stream, listed below.</p>
<h4 id="maxcount" tabindex="-1"><a class="header-anchor" href="#maxcount"><span>maxCount</span></a></h4>
<p>Passing in the max count will limit the number of events returned.</p>
<h4 id="resolvelinktos" tabindex="-1"><a class="header-anchor" href="#resolvelinktos"><span>resolveLinkTos</span></a></h4>
<p>When using projections to create new events, you can set whether the generated events are pointers to existing events. Setting this value to <code v-pre>true</code> tells KurrentDB to return the event as well as the event linking to it.</p>
<h4 id="configureoperationoptions" tabindex="-1"><a class="header-anchor" href="#configureoperationoptions"><span>configureOperationOptions</span></a></h4>
<p>You can use the <code v-pre>configureOperationOptions</code> argument to provide a function that will customise settings for each operation.</p>
<h4 id="usercredentials" tabindex="-1"><a class="header-anchor" href="#usercredentials"><span>userCredentials</span></a></h4>
<p>The <code v-pre>userCredentials</code> argument is optional. It is used to override the default credentials specified when creating the client instance.</p>
<p>@<a href="@grpc:reading-events/Program.cs">code{overriding-user-credentials}</a></p>
<h3 id="reading-from-a-revision" tabindex="-1"><a class="header-anchor" href="#reading-from-a-revision"><span>Reading from a revision</span></a></h3>
<p>Instead of providing the <code v-pre>StreamPosition</code> you can also provide a specific stream revision as a <em>big int</em> (unsigned 64-bit integer).</p>
<p>@<a href="@grpc:reading-events/Program.cs">code{read-from-stream-position}</a></p>
<h3 id="reading-backwards" tabindex="-1"><a class="header-anchor" href="#reading-backwards"><span>Reading backwards</span></a></h3>
<p>In addition to reading a stream forwards, streams can be read backwards. To read all the events backwards, set the <em>stream position</em> to the end:</p>
<p>@<a href="@grpc:reading-events/Program.cs">code{reading-backwards}</a></p>
<div class="hint-container tip">
<p class="hint-container-title">Tips</p>
<p>Read one event backwards to find the last position in the stream.</p>
</div>
<h3 id="checking-if-the-stream-exists" tabindex="-1"><a class="header-anchor" href="#checking-if-the-stream-exists"><span>Checking if the stream exists</span></a></h3>
<p>Reading a stream returns a <code v-pre>ReadStreamResult</code>, which contains a property <code v-pre>ReadState</code>. This property can have the value <code v-pre>StreamNotFound</code> or <code v-pre>Ok</code>.</p>
<p>It is important to check the value of this field before attempting to iterate an empty stream, as it will throw an exception.</p>
<p>For example:</p>
<p>@<a href="@grpc:reading-events/Program.cs">code{checking-for-stream-presence}</a></p>
<h2 id="reading-from-the-all-stream" tabindex="-1"><a class="header-anchor" href="#reading-from-the-all-stream"><span>Reading from the $all stream</span></a></h2>
<p>Reading from the <code v-pre>$all</code> stream is similar to reading from an individual stream, but please note there are differences. One significant difference is the need to provide admin user account credentials to read from the <code v-pre>$all</code> stream.  Additionally, you need to provide a transaction log position instead of a stream revision when reading from the <code v-pre>$all</code> stream.</p>
<h3 id="reading-forwards-1" tabindex="-1"><a class="header-anchor" href="#reading-forwards-1"><span>Reading forwards</span></a></h3>
<p>The simplest way to read the <code v-pre>$all</code> stream forwards is to supply a read direction and the transaction log position from which you want to start. The transaction log postion can either be a <em>stream position</em> <code v-pre>Start</code> or a <em>big int</em> (unsigned 64-bit integer):</p>
<p>@<a href="@grpc:reading-events/Program.cs">code{read-from-all-stream}</a></p>
<p>You can iterate asynchronously through the result:</p>
<p>@<a href="@grpc:reading-events/Program.cs">code{read-from-all-stream-iterate}</a></p>
<p>There are a number of additional arguments you can provide when reading the <code v-pre>$all</code> stream.</p>
<h4 id="maxcount-1" tabindex="-1"><a class="header-anchor" href="#maxcount-1"><span>maxCount</span></a></h4>
<p>Passing in the max count allows you to limit the number of events that returned.</p>
<h4 id="resolvelinktos-1" tabindex="-1"><a class="header-anchor" href="#resolvelinktos-1"><span>resolveLinkTos</span></a></h4>
<p>When using projections to create new events you can set whether the generated events are pointers to existing events. Setting this value to true will tell KurrentDB to return the event as well as the event linking to it.</p>
<p>@<a href="@grpc:reading-events/Program.cs">code{read-from-all-stream-resolving-link-Tos}</a></p>
<h4 id="configureoperationoptions-1" tabindex="-1"><a class="header-anchor" href="#configureoperationoptions-1"><span>configureOperationOptions</span></a></h4>
<p>This argument is generic setting class for all operations that can be set on all operations executed against KurrentDB.</p>
<h4 id="usercredentials-1" tabindex="-1"><a class="header-anchor" href="#usercredentials-1"><span>userCredentials</span></a></h4>
<p>The credentials used to read the data can be used by the subscription as follows. This will override the default credentials set on the connection.</p>
<p>@<a href="@grpc:reading-events/Program.cs">code{read-all-overriding-user-credentials}</a></p>
<h3 id="reading-backwards-1" tabindex="-1"><a class="header-anchor" href="#reading-backwards-1"><span>Reading backwards</span></a></h3>
<p>In addition to reading the <code v-pre>$all</code> stream forwards, it can be read backwards. To read all the events backwards, set the <em>position</em> to the end:</p>
<p>@<a href="@grpc:reading-events/Program.cs">code{read-from-all-stream-backwards}</a></p>
<div class="hint-container tip">
<p class="hint-container-title">Tips</p>
<p>Read one event backwards to find the last position in the <code v-pre>$all</code> stream.</p>
</div>
<h3 id="handling-system-events" tabindex="-1"><a class="header-anchor" href="#handling-system-events"><span>Handling system events</span></a></h3>
<p>KurrentDB will also return system events when reading from the <code v-pre>$all</code> stream. In most cases you can ignore these events.</p>
<p>All system events begin with <code v-pre>$</code> or <code v-pre>$$</code> and can be easily ignored by checking the <code v-pre>EventType</code> property.</p>
<p>@<a href="@grpc:reading-events/Program.cs">code{ignore-system-events}</a></p>
</div></template>


