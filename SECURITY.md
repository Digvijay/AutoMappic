# Security Policy

## Reporting Security Vulnerabilities

If you discover a security vulnerability in AutoMappic, please report it responsibly by emailing **security @ digvijay dot dev**.

**Please do NOT report security vulnerabilities through public GitHub issues, discussions, or pull requests.**

### What to Include in Your Report

To help us respond effectively, please include:

- A clear description of the vulnerability
- Steps to reproduce the issue
- Potential impact and severity
- Any suggested fixes (optional)
- Your contact information for follow-up

### Response Timeline

We will acknowledge receipt of your report within 48 hours and provide a more detailed response within 7 days indicating our next steps.
We will keep you informed about our progress throughout the process of fixing the vulnerability.

## Supported Versions

We actively support the following versions with security updates:

| Version | Supported          |
| ------- | ------------------ |
| 1.x.x   | :white_check_mark: |
| 0.x.x   | :white_check_mark: |

## Disclosure Policy

- We follow a coordinated disclosure process
- Vulnerabilities will be disclosed publicly only after a fix has been released
- We will credit researchers who report vulnerabilities
- We aim to release fixes within 90 days of receiving a report

## Security Best Practices for AutoMappic Users

Since AutoMappic generates C# code at compile-time, there are no runtime `System.Reflection.Emit` operations, meaning AutoMappic eliminates many reflection-based injection vectors right out of the box.

However, when mapping User Input/DTOs directly to Database Entities, you should still ensure you do not map sensitive properties arbitrarily.

```csharp
// GOOD: Explicitly ignore sensitive ID overrides
CreateMap<UserDto, User>()
    .ForMemberIgnore(dest => dest.AdminRoleId);
```

### Contact

For security-related questions or concerns:
- **Email:** security @ digvijay dot dev
- **GitHub Issues:** For non-sensitive security improvements and questions

Thank you for helping keep AutoMappic and its users secure!
