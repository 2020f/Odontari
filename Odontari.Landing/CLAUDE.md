# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Commands

```bash
# Development server
npm run dev

# Production build (static export to /out)
npm run build

# Lint
npm run lint
```

> `npm run build` is the reliable way to verify correctness — the project uses `output: "export"` so there is no `next start`.

## Architecture

**Odontari.Landing** is a Next.js 14 static landing page (`output: "export"`) for the Odontari dental SaaS product. It exports to `/out` and has no server-side logic.

### Stack

- **Next.js 14** — App Router, all pages static
- **Tailwind CSS 3** — utility-first, config in `tailwind.config.ts`
- **TypeScript**
- No state management library, no animation library — vanilla React + CSS transitions

### Design system

Defined in `tailwind.config.ts` and `app/globals.css`:

| Token | Value |
|---|---|
| `navy-deep` | `#001D39` (dominant dark background) |
| `navy-mid` | `#0A4174` |
| `teal` | `#4E8EA2` (primary accent) |
| `sky` | `#7BBDE8` (secondary accent) |
| `pale` | `#BDD8E9` |
| `pearl` | `#F4F8FB` (light section background) |
| `font-display` | Sora — used for all headings/titles |
| `font-sans` | DM Sans — body text |

Custom utilities in `globals.css`: `bg-dot-grid`, `bg-line-grid`, `text-gradient-sky`, `section-eyebrow`, `card-hover`, `teal-accent-bar`, `glass-dark`, `teal-glow`.

### Section backgrounds and nav detection

The nav reads `data-light` attributes on `<section>` elements to switch between its transparent (over dark sections) and white (over light sections) state. **Every section with a white or pearl background must have `data-light` on its `<section>` tag.** Dark sections (navy-deep, navy-mid) must not have `data-light`.

### Scroll reveal pattern

All sections use the same IntersectionObserver pattern — observe the section ref, then stagger `.animate-on-scroll` children by adding `.visible` with `setTimeout` offsets. The CSS for this lives in `globals.css`.

### Images and assets

- Static assets go in `public/`
- Use plain `<img>` tags (not `next/image`) for SVG files — `next/image` requires `images: { unoptimized: true }` (already set) for static export but SVGs work better as `<img src="/file.svg">`
- Logo: `public/logo.svg` (vector, transparent background)

### Environment variables

All prefixed `NEXT_PUBLIC_`:

| Variable | Default | Usage |
|---|---|---|
| `NEXT_PUBLIC_APP_URL` | `http://localhost:5000` | Links to the main Odontari.Web app |
| `NEXT_PUBLIC_WHATSAPP` | `18091234567` | WhatsApp CTA links |
| `NEXT_PUBLIC_CONTACT_EMAIL` | `soporte@odontari.com` | Contact email |
| `NEXT_PUBLIC_FORM_ENDPOINT` | _(unset)_ | If unset, contact form falls back to `mailto:` |
