"use client";

import { useEffect, useState } from "react";

const APP_URL = process.env.NEXT_PUBLIC_APP_URL || "http://localhost:5000";

const navLinks = [
  { label: "Características", href: "#features" },
  { label: "Planes", href: "#planes" },
  { label: "FAQ", href: "#faq" },
  { label: "Contacto", href: "#contacto" },
];

export default function Nav() {
  const [menuOpen, setMenuOpen] = useState(false);
  const [overLight, setOverLight] = useState(false);

  useEffect(() => {
    const NAV_H = 68;

    const check = () => {
      const lights = document.querySelectorAll<HTMLElement>("[data-light]");
      let light = false;
      lights.forEach((el) => {
        const r = el.getBoundingClientRect();
        // El elemento cubre el área del nav
        if (r.top < NAV_H && r.bottom > 0) light = true;
      });
      setOverLight(light);
    };

    window.addEventListener("scroll", check, { passive: true });
    check();
    return () => window.removeEventListener("scroll", check);
  }, []);

  const isLight = overLight || menuOpen;

  return (
    <nav
      className={`fixed top-0 left-0 right-0 z-50 transition-all duration-300 ${
        isLight
          ? "bg-white/95 backdrop-blur-xl border-b border-slate/15 shadow-[0_2px_16px_rgba(0,0,0,0.08)]"
          : "bg-transparent"
      }`}
    >
      <div className="max-w-6xl mx-auto px-6 h-[68px] flex items-center justify-between">

        {/* Logo */}
        <a href="/" className="flex items-center gap-2 group shrink-0">
          <img
            src="/logo.svg"
            alt="Odontari logo"
            width={34}
            height={34}
            className="w-9 h-9 object-contain shrink-0"
          />
          <span className={`font-display font-semibold text-[1.1rem] tracking-tight transition-colors duration-300
                            ${isLight ? "text-navy-deep" : "text-white"}`}>
            Odontari
          </span>
        </a>

        {/* Links — desktop */}
        <div className="hidden md:flex items-center gap-8">
          {navLinks.map((l) => (
            <a
              key={l.href}
              href={l.href}
              className={`text-sm font-medium transition-colors duration-200
                         relative after:absolute after:bottom-0 after:left-0 after:h-px after:w-0
                         after:bg-teal after:transition-all hover:after:w-full after:duration-200
                         ${isLight
                           ? "text-navy-deep/60 hover:text-navy-deep"
                           : "text-white/60 hover:text-white"}`}
            >
              {l.label}
            </a>
          ))}
        </div>

        {/* CTA — desktop */}
        <a
          href={`${APP_URL}/Identity/Account/Login`}
          className={`hidden md:inline-flex items-center gap-1.5 px-5 py-2.5 rounded-lg
                     text-sm font-semibold transition-all duration-200 hover:-translate-y-px
                     ${isLight
                       ? "bg-navy-deep hover:bg-navy-mid text-white shadow-[0_4px_12px_rgba(0,29,57,0.15)]"
                       : "bg-teal hover:bg-teal-light text-white shadow-[0_4px_12px_rgba(78,142,162,0.3)]"}`}
        >
          Iniciar sesión
          <svg className="w-3.5 h-3.5" viewBox="0 0 16 16" fill="none" stroke="currentColor" strokeWidth="2">
            <path d="M3 8h10M9 4l4 4-4 4" strokeLinecap="round" strokeLinejoin="round" />
          </svg>
        </a>

        {/* Hamburger — mobile */}
        <button
          onClick={() => setMenuOpen(!menuOpen)}
          className={`md:hidden w-10 h-10 flex items-center justify-center rounded-lg
                     transition-colors ${isLight
                       ? "text-navy-deep/70 hover:text-navy-deep hover:bg-slate/10"
                       : "text-white/70 hover:text-white hover:bg-white/10"}`}
          aria-label="Menú"
        >
          {menuOpen ? (
            <svg className="w-5 h-5" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2">
              <line x1="18" y1="6" x2="6" y2="18" /><line x1="6" y1="6" x2="18" y2="18" />
            </svg>
          ) : (
            <svg className="w-5 h-5" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2">
              <line x1="3" y1="7" x2="21" y2="7" /><line x1="3" y1="12" x2="17" y2="12" /><line x1="3" y1="17" x2="21" y2="17" />
            </svg>
          )}
        </button>
      </div>

      {/* Mobile menu */}
      {menuOpen && (
        <div className="md:hidden border-t border-slate/15 bg-white px-6 py-5 space-y-1">
          {navLinks.map((l) => (
            <a
              key={l.href}
              href={l.href}
              onClick={() => setMenuOpen(false)}
              className="flex items-center h-11 text-navy-deep/70 hover:text-navy-deep text-sm
                         font-medium px-3 rounded-lg hover:bg-slate/8 transition-colors"
            >
              {l.label}
            </a>
          ))}
          <div className="pt-2">
            <a
              href={`${APP_URL}/Identity/Account/Login`}
              className="flex items-center justify-center h-11 rounded-lg bg-navy-deep
                         text-white text-sm font-semibold hover:bg-navy-mid transition-colors"
            >
              Iniciar sesión
            </a>
          </div>
        </div>
      )}
    </nav>
  );
}
