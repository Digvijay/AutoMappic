import { defineConfig } from 'vitepress'

export default defineConfig({
  title: "AutoMappic",
  description: "Zero-Reflection. Zero-Overhead. Native AOT-First Object-to-Object Mapper for .NET 9+.",
  // Use the repo name or the actual domain if it's hosted at the root of a custom domain
  // The user said: "hosted at https://auomappic.digvijay.dev via github pages"
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
          { text: 'Benchmarks', link: '/benchmarks' },
          { text: 'Changelog', link: '/changelog' },
          { text: 'Compiler Learnings', link: '/learnings' }
        ]
      }
    ],
    socialLinks: [
      { icon: 'github', link: 'https://github.com/Digvijay/AutoMappic' }
    ]
  }
})
