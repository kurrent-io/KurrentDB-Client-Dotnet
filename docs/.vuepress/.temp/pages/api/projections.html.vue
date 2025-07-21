<template><div><h1 id="projection-management" tabindex="-1"><a class="header-anchor" href="#projection-management"><span>Projection management</span></a></h1>
<p>The various gRPC client APIs include dedicated clients that allow you to manage projections.</p>
<p>For a detailed explanation of projections, see the <RouteLink to="/server/features/projections/">server documentation</RouteLink>.</p>
<p>You can find the full sample code from this documentation page in the respective <a href="https://github.com/kurrent-io/?q=client" target="_blank" rel="noopener noreferrer">clients repositories</a>.</p>
<h2 id="creating-a-client" tabindex="-1"><a class="header-anchor" href="#creating-a-client"><span>Creating a client</span></a></h2>
<p>Projection management operations are exposed through a dedicated client.</p>
<p>@<a href="@grpc:projection-management/Program.cs">code{createClient}</a></p>
<h2 id="create-a-projection" tabindex="-1"><a class="header-anchor" href="#create-a-projection"><span>Create a projection</span></a></h2>
<p>Creates a projection that runs until the last event in the store, and then continues processing new events as they are appended to the store. The query parameter contains the JavaScript you want created as a projection.
Projections have explicit names, and you can enable or disable them via this name.</p>
<p>@<a href="@grpc:projection-management/Program.cs">code{CreateContinuous}</a></p>
<p>Trying to create projections with the same name will result in an error:</p>
<p>@<a href="@grpc:projection-management/Program.cs">code{CreateContinuous_Conflict}</a></p>
<h2 id="restart-the-subsystem" tabindex="-1"><a class="header-anchor" href="#restart-the-subsystem"><span>Restart the subsystem</span></a></h2>
<p>It is possible to restart the entire projection subsystem using the projections management client API. The user must be in the <code v-pre>$ops</code> or <code v-pre>$admin</code> group to perform this operation.</p>
<p>@<a href="@grpc:projection-management/Program.cs">code{RestartSubSystem}</a></p>
<h2 id="enable-a-projection" tabindex="-1"><a class="header-anchor" href="#enable-a-projection"><span>Enable a projection</span></a></h2>
<p>Enables an existing projection by name.
Once enabled, the projection will start to process events even after restarting the server or the projection subsystem.
You must have access to a projection to enable it, see the <RouteLink to="/server/security/user-authorization.html">ACL documentation</RouteLink>.</p>
<p>@<a href="@grpc:projection-management/Program.cs">code{Enable}</a></p>
<p>You can only enable an existing projection. When you try to enable a non-existing projection, you'll get an error:</p>
<p>@<a href="@grpc:projection-management/Program.cs">code{EnableNotFound}</a></p>
<h2 id="disable-a-projection" tabindex="-1"><a class="header-anchor" href="#disable-a-projection"><span>Disable a projection</span></a></h2>
<p>Disables a projection, this will save the projection checkpoint.
Once disabled, the projection will not process events even after restarting the server or the projection subsystem.
You must have access to a projection to disable it, see the <RouteLink to="/server/security/user-authorization.html">ACL documentation</RouteLink>.</p>
<p>@<a href="@grpc:projection-management/Program.cs">code{Disable}</a></p>
<p>You can only disable an existing projection. When you try to disable a non-existing projection, you'll get an error:</p>
<p>@<a href="@grpc:projection-management/Program.cs">code{DisableNotFound}</a></p>
<h2 id="delete-a-projection" tabindex="-1"><a class="header-anchor" href="#delete-a-projection"><span>Delete a projection</span></a></h2>
<p>This feature is not available for this client.</p>
<h2 id="abort-a-projection" tabindex="-1"><a class="header-anchor" href="#abort-a-projection"><span>Abort a projection</span></a></h2>
<p>Aborts a projection, this will not save the projection's checkpoint.</p>
<p>@<a href="@grpc:projection-management/Program.cs">code{Abort}</a></p>
<p>You can only abort an existing projection. When you try to abort a non-existing projection, you'll get an error:</p>
<p>@<a href="@grpc:projection-management/Program.cs">code{Abort_NotFound}</a></p>
<h2 id="reset-a-projection" tabindex="-1"><a class="header-anchor" href="#reset-a-projection"><span>Reset a projection</span></a></h2>
<p>Resets a projection, which causes deleting the projection checkpoint. This will force the projection to start afresh and re-emit events. Streams that are written to from the projection will also be soft-deleted.</p>
<p>@<a href="@grpc:projection-management/Program.cs">code{Reset}</a></p>
<p>Resetting a projection that does not exist will result in an error.</p>
<p>@<a href="@grpc:projection-management/Program.cs">code{Reset_NotFound}</a></p>
<h2 id="update-a-projection" tabindex="-1"><a class="header-anchor" href="#update-a-projection"><span>Update a projection</span></a></h2>
<p>Updates a projection with a given name. The query parameter contains the new JavaScript. Updating system projections using this operation is not supported at the moment.</p>
<p>@<a href="@grpc:projection-management/Program.cs">code{Update}</a></p>
<p>You can only update an existing projection. When you try to update a non-existing projection, you'll get an error:</p>
<p>@<a href="@grpc:projection-management/Program.cs">code{Update_NotFound}</a></p>
<h2 id="list-all-projections" tabindex="-1"><a class="header-anchor" href="#list-all-projections"><span>List all projections</span></a></h2>
<p>Returns a list of all projections, user defined &amp; system projections.
See the <a href="#projection-details">projection details</a> section for an explanation of the returned values.</p>
<p>@<a href="@grpc:projection-management/Program.cs">code{ListAll}</a></p>
<h2 id="list-continuous-projections" tabindex="-1"><a class="header-anchor" href="#list-continuous-projections"><span>List continuous projections</span></a></h2>
<p>Returns a list of all continuous projections.
See the <a href="#projection-details">projection details</a> section for an explanation of the returned values.</p>
<p>@<a href="@grpc:projection-management/Program.cs">code{ListContinuous}</a></p>
<h2 id="get-status" tabindex="-1"><a class="header-anchor" href="#get-status"><span>Get status</span></a></h2>
<p>Gets the status of a named projection.
See the <a href="#projection-details">projection details</a> section for an explanation of the returned values.</p>
<p>@<a href="@grpc:projection-management/Program.cs">code{GetStatus}</a></p>
<h2 id="get-state" tabindex="-1"><a class="header-anchor" href="#get-state"><span>Get state</span></a></h2>
<p>Retrieves the state of a projection.</p>
<p>@<a href="@grpc:projection-management/Program.cs">code{GetState}</a></p>
<h2 id="get-result" tabindex="-1"><a class="header-anchor" href="#get-result"><span>Get result</span></a></h2>
<p>Retrieves the result of the named projection and partition.</p>
<p>@<a href="@grpc:projection-management/Program.cs">code{GetResult}</a></p>
<h2 id="projection-details" tabindex="-1"><a class="header-anchor" href="#projection-details"><span>Projection Details</span></a></h2>
<p><a href="#list-all-projections">List all</a>, <a href="#list-continuous-projections">list continuous</a> and <a href="#get-status">get status</a> all return the details and statistics of projections</p>
<table>
<thead>
<tr>
<th>Field</th>
<th>Description</th>
</tr>
</thead>
<tbody>
<tr>
<td><code v-pre>Name</code>, <code v-pre>EffectiveName</code></td>
<td>The name of the projection</td>
</tr>
<tr>
<td><code v-pre>Status</code></td>
<td>A human readable string of the current statuses of the projection (see below)</td>
</tr>
<tr>
<td><code v-pre>StateReason</code></td>
<td>A human readable string explaining the reason of the current projection state</td>
</tr>
<tr>
<td><code v-pre>CheckpointStatus</code></td>
<td>A human readable string explaining the current operation performed on the checkpoint : <code v-pre>requested</code>, <code v-pre>writing</code></td>
</tr>
<tr>
<td><code v-pre>Mode</code></td>
<td><code v-pre>Continuous</code>, <code v-pre>OneTime</code> , <code v-pre>Transient</code></td>
</tr>
<tr>
<td><code v-pre>CoreProcessingTime</code></td>
<td>The total time, in ms, the projection took to handle events since the last restart</td>
</tr>
<tr>
<td><code v-pre>Progress</code></td>
<td>The progress, in %, indicates how far this projection has processed event, in case of a restart this could be -1% or some number. It will be updated as soon as a new event is appended and processed</td>
</tr>
<tr>
<td><code v-pre>WritesInProgress</code></td>
<td>The number of write requests to emitted streams currently in progress, these writes can be batches of events</td>
</tr>
<tr>
<td><code v-pre>ReadsInProgress</code></td>
<td>The number of read requests currently in progress</td>
</tr>
<tr>
<td><code v-pre>PartitionsCached</code></td>
<td>The number of cached projection partitions</td>
</tr>
<tr>
<td><code v-pre>Position</code></td>
<td>The Position of the last processed event</td>
</tr>
<tr>
<td><code v-pre>LastCheckpoint</code></td>
<td>The Position of the last checkpoint of this projection</td>
</tr>
<tr>
<td><code v-pre>EventsProcessedAfterRestart</code></td>
<td>The number of events processed since the last restart of this projection</td>
</tr>
<tr>
<td><code v-pre>BufferedEvents</code></td>
<td>The number of events in the projection read buffer</td>
</tr>
<tr>
<td><code v-pre>WritePendingEventsBeforeCheckpoint</code></td>
<td>The number of events waiting to be appended to emitted streams before the pending checkpoint can be written</td>
</tr>
<tr>
<td><code v-pre>WritePendingEventsAfterCheckpoint</code></td>
<td>The number of events to be appended to emitted streams since the last checkpoint</td>
</tr>
<tr>
<td><code v-pre>Version</code></td>
<td>This is used internally, the version is increased when the projection is edited or reset</td>
</tr>
<tr>
<td><code v-pre>Epoch</code></td>
<td>This is used internally, the epoch is increased when the projection is reset</td>
</tr>
</tbody>
</table>
<p>The <code v-pre>Status</code> string is a combination of the following values.
The first 3 are the most common one, as the other one are transient values while the projection is initialised or stopped</p>
<table>
<thead>
<tr>
<th>Value</th>
<th>Description</th>
</tr>
</thead>
<tbody>
<tr>
<td>Running</td>
<td>The projection is running and processing events</td>
</tr>
<tr>
<td>Stopped</td>
<td>The projection is stopped and is no longer processing new events</td>
</tr>
<tr>
<td>Faulted</td>
<td>An error occurred in the projection, <code v-pre>StateReason</code> will give the fault details, the projection is not processing events</td>
</tr>
<tr>
<td>Initial</td>
<td>This is the initial state, before the projection is fully initialised</td>
</tr>
<tr>
<td>Suspended</td>
<td>The projection is suspended and will not process events, this happens while stopping the projection</td>
</tr>
<tr>
<td>LoadStateRequested</td>
<td>The state of the projection is being retrieved, this happens while the projection is starting</td>
</tr>
<tr>
<td>StateLoaded</td>
<td>The state of the projection is loaded, this happens while the projection is starting</td>
</tr>
<tr>
<td>Subscribed</td>
<td>The projection has successfully subscribed to its readers, this happens while the projection is starting</td>
</tr>
<tr>
<td>FaultedStopping</td>
<td>This happens before the projection is stopped due to an error in the projection</td>
</tr>
<tr>
<td>Stopping</td>
<td>The projection is being stopped</td>
</tr>
<tr>
<td>CompletingPhase</td>
<td>This happens while the projection is stopping</td>
</tr>
<tr>
<td>PhaseCompleted</td>
<td>This happens while the projection is stopping</td>
</tr>
</tbody>
</table>
</div></template>


