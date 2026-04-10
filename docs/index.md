---
layout: home

hero:
  name: AutoMappic
  text: The Zero-Reflection Mapper.
  tagline: High-performance, statically generated object mapping for modern .NET 9+ workflows.
  image:
    src: /assets/hero.png
    alt: AutoMappic Hero
  actions:
    - theme: brand
      text: Get Started
      link: /getting-started
    - theme: alt
      text: Full Features Case Study
      link: /tutorials/full-features-walkthrough
    - theme: alt
      text: GitHub
      link: https://github.com/Digvijay/AutoMappic

features:
  - title: The "Ultimate" release (v0.7.0)
    details: Hardened Native AOT support with recursion guards, zero-allocation database streaming (IAsyncEnumerable), and a new fluent MapTo API.
  - title: 100% Native AOT Compatible
    details: AutoMappic now includes AOT-safety transparency and automatic recursion protection. No runtime reflection, no dynamic IL emission, and no stack overflows in complex graphs.
  - title: Database Streaming
    details: Stream millions of rows with zero-overhead using MapAsync on DbDataReader. Leverages IAsyncEnumerable for pure, non-blocking asynchronous mapping.
  - title: Standalone Mapping ([AutoMap])
    details: Eliminate Profile boilerplate for 90% of your DTOs. Just decorate your partial class with [AutoMap] and let the generator handle the implementation.
  - title: Rigorous Diagnostic Suite
    details: Build-time protection with AM0001-AM0018. Catch unmapped properties, fuzzy-match typos, missing constructors, and ProjectTo incompatibilities instantly.
  - title: Fluent Developer Experience
    details: Map between types naturally with `source.MapTo<Dest>(mapper)`. Statically generated, type-safe, and incredibly fast.
---
