import { defineConfig } from 'vitepress'
import { withMermaid } from 'vitepress-plugin-mermaid'

export default withMermaid(defineConfig({
  title: "AutoMappic",
  description: "Zero-Reflection. Zero-Overhead. Native AOT-First Object-to-Object Mapper for .NET 9+.",
  head: [
    ['link', { rel: 'icon', href: '/favicon.ico' }],
    ['meta', { name: 'theme-color', content: '#5f67ee' }],
    ['meta', { property: 'og:type', content: 'website' }],
    ['meta', { property: 'og:locale', content: 'en' }],
    ['meta', { property: 'og:site_name', content: 'AutoMappic' }],
    ['meta', { property: 'og:image', content: 'https://automappic.digvijay.dev/og-image.png' }],
    ['meta', { name: 'twitter:card', content: 'summary_large_image' }],
    ['meta', { name: 'twitter:site', content: '@AutoMappic' }],
    ['meta', { name: 'twitter:image', content: 'https://automappic.digvijay.dev/og-image.png' }],
    ['meta', { name: 'keywords', content: 'AutoMappic, .NET 10, .NET 9, Source Generator, Object Mapper, Native AOT, Performance, Zero Reflection, EF Core Sync, C# mapping' }],
    [
      'script',
      { type: 'application/ld+json' },
      JSON.stringify({
        "@context": "https://schema.org",
        "@type": "SoftwareApplication",
        "name": "AutoMappic",
        "operatingSystem": "Windows, macOS, Linux",
        "applicationCategory": "DeveloperApplication",
        "screenshot": "https://automappic.digvijay.dev/og-image.png",
        "softwareVersion": "0.5.1",
        "author": {
          "@type": "Organization",
          "name": "AutoMappic OSS",
          "url": "https://github.com/Digvijay/AutoMappic"
        },
        "offers": {
          "@type": "Offer",
          "price": "0",
          "priceCurrency": "USD"
        }
      })
    ]
  ],
  sitemap: {
    hostname: 'https://automappic.digvijay.dev'
  },
  base: '/',
  themeConfig: {
    nav: [
      { text: 'Home', link: '/' },
      { text: 'Getting Started', link: '/getting-started' },
    ],
    sidebar: [
      {
        text: 'Guide',
        items: [
          { text: 'Getting Started', link: '/getting-started' },
          { text: 'Asynchronous Mapping', link: '/asynchronous-mapping' },
          { text: 'How it Works', link: '/how-it-works' },
          { text: 'Sustainability & ESG', link: '/sustainability' }
        ]
      },
      {
        text: 'Tutorials',
        items: [
          { text: 'Basic Mapping', link: '/tutorials/basic-mapping' },
          { text: 'Advanced Configuration', link: '/tutorials/advanced-configuration' },
          { text: 'Conditional Mapping', link: '/tutorials/conditional-mapping' },
          { text: 'Custom Constructors', link: '/tutorials/custom-constructors' },
          { text: 'Async Lifecycle Hooks', link: '/tutorials/async-lifecycle-hooks' },
          { text: 'Collections & Dictionaries', link: '/tutorials/collections-and-dictionaries' },
          { text: 'Unit Testing Your Mappings', link: '/tutorials/unit-testing' },
          { text: 'Queryable Projection', link: '/tutorials/queryable-projection' },
          { text: 'DataReader Mapping', link: '/tutorials/data-reader-mapping' },
          { text: 'Value Converters', link: '/tutorials/value-converters' },
          { text: 'Naming Conventions', link: '/tutorials/naming-conventions' },
          { text: 'Performance Profiling', link: '/tutorials/performance-profiling' },
          { text: 'CLI Tools & Visualization', link: '/tutorials/cli-tools' },
          { text: 'Native AOT Guide', link: '/tutorials/native-aot-guide' },
          { text: 'Porting from AutoMapper', link: '/tutorials/porting-from-automapper' },
          { text: 'Identity Management', link: '/tutorials/identity-management' },
          { text: 'EF Core Smart-Sync API', link: '/tutorials/ef-core-smart-sync' },
          { text: 'Static Converters', link: '/tutorials/static-converters' }
        ]
      },
      {
        text: 'Reference',
        items: [
          { text: 'Mapping API', link: '/mapping-api' },
          { text: 'Diagnostic Suite', link: '/diagnostics' },
          { text: 'Reference Apps', link: '/reference-apps' },
          { text: 'Benchmarks', link: '/benchmarks' },
          { text: 'Roadmap', link: '/roadmap' },
          { text: 'Changelog', link: '/changelog' }
        ]
      }
    ],
    socialLinks: [
      { icon: 'github', link: 'https://github.com/Digvijay/AutoMappic' }
    ]
  }
}))
