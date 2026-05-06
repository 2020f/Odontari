"use client";

import { useEffect, useRef, useState } from "react";

const plans = [
  {
    name: "Básico",
    priceRD: "RD$3,000",
    priceUSD: "$50",
    period: "/mes",
    description: "Ideal para clínicas pequeñas con 1–2 usuarios.",
    highlight: false,
    badge: null,
    features: [
      "1–2 usuarios activos",
      "Gestión de pacientes",
      "Agenda inteligente",
      "Historial clínico",
      "Facturación NCF básica",
      "Soporte por correo",
    ],
    missing: [
      "Consentimiento informado digital",
      "Reportes financieros avanzados",
      "Exportación Excel/PDF",
    ],
  },
  {
    name: "Profesional",
    priceRD: "RD$6,000",
    priceUSD: "$100",
    period: "/mes",
    description: "Para clínicas en crecimiento con múltiples doctores.",
    highlight: true,
    badge: "Más popular",
    features: [
      "Hasta 5 usuarios",
      "Todo el plan Básico",
      "Consentimiento informado digital",
      "Odontograma y periodontograma",
      "Facturación NCF completa",
      "Reportes financieros avanzados",
      "Exportación Excel/PDF",
      "Soporte prioritario",
    ],
    missing: [],
  },
  {
    name: "Premium",
    priceRD: "RD$12,000",
    priceUSD: "$200",
    period: "/mes",
    description: "Para grandes clínicas y grupos odontológicos.",
    highlight: false,
    badge: null,
    features: [
      "Usuarios ilimitados",
      "Todo el plan Profesional",
      "Multi-sucursal (próximamente)",
      "Automatizaciones avanzadas",
      "Analítica avanzada",
      "Almacenamiento extendido",
      "Onboarding personalizado",
      "Soporte dedicado 24/7",
    ],
    missing: [],
  },
];

const APP_URL = process.env.NEXT_PUBLIC_APP_URL || "http://localhost:5000";

export default function Pricing() {
  const ref = useRef<HTMLDivElement>(null);
  const [usd, setUsd] = useState(false);

  useEffect(() => {
    const el = ref.current;
    if (!el) return;
    const observer = new IntersectionObserver(
      (entries) => {
        entries.forEach((entry) => {
          if (entry.isIntersecting) {
            el.querySelectorAll<HTMLElement>(".animate-on-scroll").forEach((item, i) =>
              setTimeout(() => item.classList.add("visible"), i * 65)
            );
            observer.unobserve(entry.target);
          }
        });
      },
      { threshold: 0.06 }
    );
    observer.observe(el);
    return () => observer.disconnect();
  }, []);

  return (
    <section data-light id="planes" ref={ref} className="py-24 bg-white">
      <div className="max-w-6xl mx-auto px-6">
        {/* Header */}
        <div className="text-center mb-12">
          <p className="animate-on-scroll section-eyebrow text-teal mb-4">
            Planes
          </p>
          <h2 className="animate-on-scroll font-display font-bold text-navy-deep
                         text-3xl sm:text-4xl leading-tight mb-4">
            Diseñados para el crecimiento
          </h2>
          <p className="animate-on-scroll text-slate text-base max-w-md mx-auto mb-8">
            Sin contratos de largo plazo. Cancela cuando quieras.
          </p>

          {/* Currency toggle */}
          <div className="animate-on-scroll inline-flex items-center gap-3 px-5 py-2.5 rounded-full
                          bg-pearl border border-slate/15">
            <span className={`text-sm font-medium transition-colors ${!usd ? "text-navy-deep" : "text-slate/50"}`}>
              RD$ Pesos
            </span>
            <button
              onClick={() => setUsd(!usd)}
              style={{ width: 44, height: 24, minWidth: 44, flexShrink: 0 }}
              className={`relative rounded-full transition-colors duration-200 focus:outline-none
                          ${usd ? "bg-teal" : "bg-slate/25"}`}
              aria-label="Cambiar moneda"
            >
              <span
                style={{
                  position: "absolute",
                  top: 4,
                  left: usd ? 24 : 4,
                  width: 16,
                  height: 16,
                  borderRadius: "50%",
                  background: "white",
                  boxShadow: "0 1px 3px rgba(0,0,0,0.2)",
                  transition: "left 0.2s",
                }}
              />
            </button>
            <span className={`text-sm font-medium transition-colors ${usd ? "text-navy-deep" : "text-slate/50"}`}>
              USD
            </span>
          </div>
        </div>

        {/* Cards */}
        <div className="grid grid-cols-1 md:grid-cols-3 gap-6 items-end">
          {plans.map((plan, i) => (
            <PlanCard key={i} plan={plan} appUrl={APP_URL} usd={usd} />
          ))}
        </div>

        <p className="animate-on-scroll text-center text-slate/60 text-sm mt-10">
          ¿Necesitas algo personalizado?{" "}
          <a href="#contacto" className="text-teal font-medium underline underline-offset-2 hover:text-teal-light transition-colors">
            Contáctanos
          </a>{" "}
          y armamos un plan a tu medida.
        </p>
      </div>
    </section>
  );
}

function PlanCard({ plan, appUrl, usd }: {
  plan: (typeof plans)[0];
  appUrl: string;
  usd: boolean;
}) {
  return (
    <div
      className={`animate-on-scroll relative flex flex-col rounded-2xl overflow-hidden
        transition-all duration-300
        ${plan.highlight
          ? "bg-navy-deep shadow-[0_24px_60px_rgba(0,29,57,0.25)] scale-[1.03]"
          : "bg-white border border-slate/12 shadow-sm hover:shadow-md card-hover"
        }`}
    >
      {plan.badge && (
        <div className="absolute top-0 left-0 right-0 h-0.5 bg-gradient-to-r from-teal to-sky" />
      )}

      <div className={`p-7 ${plan.highlight ? "pb-5" : "pb-5"}`}>
        {plan.badge && (
          <span className="inline-block px-3 py-1 rounded-full text-[10px] font-bold
                           tracking-wider uppercase bg-teal/20 text-teal-light mb-4">
            {plan.badge}
          </span>
        )}

        <p className={`font-display font-bold text-sm mb-2
                       ${plan.highlight ? "text-teal-light" : "text-teal"}`}>
          {plan.name}
        </p>
        <div className="flex items-end gap-1 mb-2">
          <span className={`font-display font-bold text-[2.4rem] leading-none
                            ${plan.highlight ? "text-white" : "text-navy-deep"}`}>
            {usd ? plan.priceUSD : plan.priceRD}
          </span>
          <span className={`text-sm mb-1.5 ${plan.highlight ? "text-white/50" : "text-slate"}`}>
            {plan.period}
          </span>
        </div>
        <p className={`text-sm leading-snug ${plan.highlight ? "text-white/60" : "text-slate"}`}>
          {plan.description}
        </p>
      </div>

      <div className={`mx-7 h-px ${plan.highlight ? "bg-white/10" : "bg-slate/10"}`} />

      <ul className="px-7 py-5 space-y-2.5 flex-1">
        {plan.features.map((f, i) => (
          <li key={i} className="flex items-start gap-2.5 text-sm">
            <svg className={`w-4 h-4 mt-0.5 shrink-0 ${plan.highlight ? "text-teal-light" : "text-teal"}`}
                 viewBox="0 0 20 20" fill="currentColor">
              <path fillRule="evenodd" d="M16.707 5.293a1 1 0 010 1.414l-8 8a1 1 0 01-1.414 0l-4-4a1 1 0 011.414-1.414L8 12.586l7.293-7.293a1 1 0 011.414 0z" clipRule="evenodd" />
            </svg>
            <span className={plan.highlight ? "text-white/85" : "text-navy-deep/75"}>{f}</span>
          </li>
        ))}
        {plan.missing.map((f, i) => (
          <li key={i} className="flex items-start gap-2.5 text-sm opacity-30">
            <svg className="w-4 h-4 mt-0.5 shrink-0" viewBox="0 0 20 20" fill="none"
                 stroke="currentColor" strokeWidth="1.5">
              <line x1="5" y1="10" x2="15" y2="10" />
            </svg>
            <span>{f}</span>
          </li>
        ))}
      </ul>

      <div className="px-7 pb-7">
        <a
          href={`${appUrl}/Identity/Account/Login`}
          className={`block text-center py-3.5 rounded-xl font-semibold text-sm
                       transition-all duration-200
            ${plan.highlight
              ? "bg-teal hover:bg-teal-light text-white shadow-[0_8px_24px_rgba(78,142,162,0.3)]"
              : "bg-navy-deep hover:bg-navy-mid text-white"
            }`}
        >
          Comenzar ahora
        </a>
      </div>
    </div>
  );
}
