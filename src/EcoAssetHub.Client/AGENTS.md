# Repository Guidelines

## Project Overview

This repository contains the React/Vite frontend for the EcoAssetHub platform.

Tech stack:

- React 19
- TypeScript
- Vite
- Vitest

The backend is a separate .NET solution with direct API controllers and repository-backed services.

When working in this repository, focus only on the frontend unless explicitly instructed otherwise.

---

# Project Structure

```
src/
```

Contains:

- React components
- Pages
- Hooks
- Services
- Types
- Utilities
- Assets
- Tests

Important files:

- `src/main.tsx`
- `src/styles.css`
- `vite.config.ts`
- `tsconfig.json`

Keep related files together whenever possible.

Example:

```
Dashboard/
    Dashboard.tsx
    Dashboard.css
    Dashboard.test.tsx
```

Avoid creating unnecessary folders.

---

# Development Commands

Install dependencies

```bash
npm install
```

Run development server

```bash
npm start
```

Production build

```bash
npm run build
```

Preview production build

```bash
npm run preview
```

Run tests

```bash
npm test
```

Always ensure the project builds successfully before considering work complete.

---

# React Standards

Use:

- Functional components
- Hooks
- TypeScript
- Strong typing
- Composition over inheritance

Avoid:

- Class components
- any unless absolutely unavoidable
- Deep prop drilling
- Duplicate state

Prefer reusable components over duplicated markup.

---

# Component Guidelines

Components should have a single responsibility.

Prefer:

- Small components
- Reusable UI
- Clear prop interfaces
- Predictable rendering

Split large components into smaller ones when appropriate.

Move business logic into hooks or utility functions when it improves readability.

---

# TypeScript

Always:

- Use explicit interfaces for component props.
- Prefer type inference where obvious.
- Avoid unnecessary type assertions.
- Keep types close to the feature using them.
- Use readonly where appropriate.

Never disable TypeScript checks to make code compile.

---

# Styling

Create modern, polished user interfaces.

Prioritize:

- Clean layouts
- Consistent spacing
- Visual hierarchy
- Accessibility
- Responsive design
- Professional appearance

Avoid:

- Crowded layouts
- Random spacing
- Inconsistent typography
- Excessive colors
- Overly decorative effects

Prefer whitespace over borders.

Use consistent spacing throughout the application.

---

# Responsive Design

Every page should work well on:

- Mobile
- Tablet
- Desktop

Design mobile-first whenever practical.

Verify layouts around:

- 375px
- 768px
- 1024px
- 1440px

Never introduce horizontal scrolling.

---

# UI Quality

Every interface should feel production ready.

Include:

- Hover states
- Focus states
- Loading states
- Empty states
- Error states
- Disabled states where appropriate

Use smooth but subtle transitions.

Maintain consistent spacing and alignment.

---

# Accessibility

Always:

- Use semantic HTML.
- Label form controls.
- Support keyboard navigation.
- Maintain sufficient color contrast.
- Use proper buttons instead of clickable divs.
- Add meaningful alt text for images.
- Preserve visible focus indicators.

Accessibility should never be sacrificed for appearance.

---

# Performance

Prefer:

- Lazy loading where appropriate
- Memoization only when beneficial
- Efficient rendering
- Minimal unnecessary re-renders

Reuse existing utilities before creating new ones.

Avoid duplicate API requests.

---

# Code Quality

Write code that is:

- Readable
- Maintainable
- Predictable
- Reusable

Remove:

- Dead code
- Console logging
- Unused imports
- Commented-out code

Keep files organized.

---

# Testing

Use Vitest.

Write tests for:

- Rendering
- User interactions
- State changes
- Utility functions
- Custom hooks

Keep tests focused on behavior rather than implementation.

Run:

```bash
npm test
```

before completing significant work.

---

# Before Completing Any Task

Always:

1. Build the project.
2. Fix all TypeScript errors.
3. Remove unused imports.
4. Remove debugging code.
5. Ensure consistent formatting.
6. Verify responsive layouts.
7. Check loading and error states.
8. Ensure accessibility requirements are met.

---

# Design Expectations

Think like a senior frontend engineer and product designer.

Do not simply make the UI functional.

Aim for interfaces that are:

- Modern
- Clean
- Consistent
- Intuitive
- Professional

When designing pages:

- Use consistent spacing.
- Maintain a clear visual hierarchy.
- Group related content.
- Keep actions obvious.
- Reduce unnecessary visual noise.

When given a screenshot, mockup, or design reference:

- Match it closely.
- Adapt it responsively.
- Reuse existing components whenever possible.

---

# Working Style

Before making changes:

- Understand the existing codebase.
- Follow existing architectural patterns.
- Extend existing components before creating new ones.
- Keep changes focused and maintainable.

When multiple solutions exist, prefer the one that is:

- Simpler
- Easier to maintain
- Consistent with the existing project
- Easier for future developers to understand

---

# Commits

Use Conventional Commits.

Examples:

- feat:
- fix:
- refactor:
- chore:
- docs:
- test:

Keep commit messages concise and imperative.

---

# Security

Never commit:

- Secrets
- Tokens
- API keys
- Credentials
- Environment-specific configuration

Keep deployment-specific configuration in environment variables.
