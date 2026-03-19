import { defineConfig } from 'vitepress'
import { withMermaid } from 'vitepress-plugin-mermaid'

export default withMermaid(defineConfig({
  title: "AutoMappic",
  description: "Zero-Reflection. Zero-Overhead. Native AOT-First Object-to-Object Mapper for .NET 9+.",
  // Use the repo name or the actual domain if it's hosted at the root of a custom domain
  // Assuming standard GitHub Pages custom domain routing where it's at the root.
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
          { text: 'CLI Tools & Visualization', link: '/tutorials/cli-tools' },
          { text: 'Native AOT Guide', link: '/tutorials/native-aot-guide' }
        ]
      },
      {
        text: 'Reference',
        items: [
          { text: 'Mapping API', link: '/mapping-api' },
          { text: 'Diagnostic Suite', link: '/diagnostics' },
          { text: 'Reference Apps', link: '/reference-apps' },
          { text: 'Benchmarks', link: '/benchmarks' },
          { text: 'Changelog', link: '/changelog' }
        ]
      }
    ],
    socialLinks: [
      { icon: 'github', link: 'https://github.com/Digvijay/AutoMappic' }
    ]
  }
}))
