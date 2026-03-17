import { defineConfig } from 'vitepress'
import { withMermaid } from 'vitepress-plugin-mermaid'

export default withMermaid(defineConfig({
  title: "AutoMappic",
  description: "Zero-Reflection. Zero-Overhead. Native AOT-First Object-to-Object Mapper for .NET 9+.",
  // Use the repo name or the actual domain if it's hosted at the root of a custom domain
  // The user said: "hosted at https://automappic.digvijay.dev via github pages"
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
          { text: 'How it Works', link: '/how-it-works' },
          { text: 'Sustainability & ESG', link: '/sustainability' },
          { text: 'Benchmarks', link: '/benchmarks' },
          { text: 'Changelog', link: '/changelog' }
        ]
      },
      {
        text: 'Reference',
        items: [
          { text: 'Diagnostic Suite', link: '/diagnostics' },
          { text: 'Reference Apps', link: '/reference-apps' }
        ]
      }
    ],
    socialLinks: [
      { icon: 'github', link: 'https://github.com/Digvijay/AutoMappic' }
    ]
  }
}))
