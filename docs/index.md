---
layout: home

hero:
  name: AutoMappic
  text: The Zero-Reflection Mapper.
  tagline: Native AOT-First Object-to-Object mapping for .NET 9+ powered by Roslyn Interceptors.
  actions:
    - theme: brand
      text: Get Started
      link: /getting-started
    - theme: alt
      text: View on GitHub
      link: https://github.com/Digvijay/AutoMappic

features:
  - title: 100% Native AOT Compatible
    details: AutoMappic generates highly-optimized C# code at compile-time. No runtime reflection, no System.Reflection.Emit.
  - title: Drop-in Replacement
    details: Identical syntax to AutoMapper. Just change `using AutoMapper;` to `using AutoMappic;` and enjoy the performance boost.
  - title: Convention-Based Magic
    details: Automatically flattens properties (e.g. `Order.Customer.Name` to `OrderDto.CustomerName`) and handles deep collections.
---
