
# Kurrent Client V2.0: Implicit Projection Resolution

## Executive Summary

This proposal introduces a bold and user-centric evolution to the KurrentDB client API in version 2.0: the complete removal of the `resolveLinkTos` parameter for reading and subscribing to projection streams. Instead, we propose that link resolution become an implicit, server-side operation.

This isn't just an API change—it’s a game-changer. It demolishes an entire layer of needless complexity, turning what used to be an arcane configuration detail into a seamless, invisible part of the developer experience. It improves clarity, boosts performance, and aligns perfectly with what developers intuitively expect. It might just be the best idea we’ve ever had.

---

## The Problem: A Hidden Minefield of Complexity

When developers consume events from projection streams (e.g., `$et-MyEventType`, `$ce-Category`), they run into the dreaded `resolveLinkTos` parameter. Here’s what that seemingly simple boolean actually demands:

- **Understand an internal KurrentDB abstraction** — The concept of "link-to" events isn’t part of your domain model. It’s an implementation detail—and yet you’re forced to know it.
- **Write defensive logic** — Developers need to inspect both `event` and `link` fields from `ResolvedEvent` objects, adding conditionals that clutter code and introduce subtle bugs.
- **Manage projection semantics manually** — Consumers must decide, every time, whether to pull the original event or the pointer. And if they get it wrong? The app breaks silently or behaves unpredictably.

It’s a trap. And one that every client hits eventually.

---

## The Solution: Seamless, Server-Side Resolution

In version 2.0, we propose removing `resolveLinkTos` altogether. Clients will no longer need to know about it. Instead:

- When a client reads from or subscribes to a projection stream (system or user-defined) or `$all`, the server will **automatically resolve** link events before sending them.
- This resolution is done **before** transmission and with a wire protocol optimization to ensure the client receives only the original, resolved `RecordedEvent`.

No duplication. No guesswork. No trade-offs.

---

## Why This Matters: Simplicity and Speed Like Never Before

This seemingly small change unlocks **massive wins**:

- **"Just Give Me the Event!"** — Developers get exactly what they want, no knobs, no flags.
- **Cleaner Codebases** — No more branching logic, no more `ResolvedEvent` gymnastics. Just domain events.
- **Zero Cognitive Overhead** — The whole "link-to" concept fades into the background. Developers never have to think about it again.
- **Eliminate Entire Classes of Bugs** — No more silent failures from forgetting `resolveLinkTos`, or inconsistencies between projections and direct streams.
- **Blazing Fast Performance** — By resolving links server-side, we eliminate follow-up fetches, halve the bandwidth usage, and cut latency for event retrieval. It’s leaner, faster, and more reliable.

This isn't a marginal improvement. It's a tectonic shift in how clean, fast, and intuitive consuming events can be.

---

## Final Thoughts: This Is the Sports Car Moment

This change is like replacing a clunky gear shift with a smooth, automatic transmission. It lets developers focus on building great applications without getting bogged down in protocol trivia.

It’s a quality-of-life upgrade you feel instantly. Suddenly, everything is smoother. Every line of client code feels lighter. Every projection stream just works.

Let’s not overthink this. It’s the right call. Let’s ship it.

> This is what happens when you take something awkward and manual—and make it automatic, invisible, and beautiful. It’s the engineering equivalent of "of course that’s how it should work."

**Best. Idea. Ever.**
