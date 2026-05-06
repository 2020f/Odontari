"use client";

import { useEffect, useRef } from "react";

const APP_URL = process.env.NEXT_PUBLIC_APP_URL || "http://localhost:5000";
const WHATSAPP = process.env.NEXT_PUBLIC_WHATSAPP || "18091234567";

export default function Hero() {
  const ref = useRef<HTMLDivElement>(null);

  useEffect(() => {
    const el = ref.current;
    if (!el) return;
    const items = el.querySelectorAll<HTMLElement>(".animate-on-scroll");
    items.forEach((item, i) => {
      setTimeout(() => item.classList.add("visible"), 80 + i * 110);
    });
  }, []);

  return (
    <section
      ref={ref}
      className="relative min-h-screen flex items-center overflow-hidden bg-navy-deep"
    >
      {/* Background layers */}
      <div className="absolute inset-0 pointer-events-none">
        {/* Dot grid */}
        <div className="absolute inset-0 bg-dot-grid opacity-40" />
        {/* Radial gradients */}
        <div className="absolute top-1/4 -left-32 w-[600px] h-[600px] rounded-full
                        bg-teal/10 blur-[100px]" />
        <div className="absolute bottom-0 right-0 w-[500px] h-[400px]
                        bg-navy-mid/60 blur-[80px]" />
        {/* Thin diagonal line accent */}
        <div className="absolute top-0 left-0 w-full h-full overflow-hidden">
          <div className="absolute top-[-10%] left-[45%] w-px h-[130%]
                          bg-gradient-to-b from-transparent via-teal/20 to-transparent
                          rotate-[15deg] origin-top" />
        </div>
      </div>

      <div className="relative z-10 max-w-6xl mx-auto px-6 pt-28 pb-20
                      grid grid-cols-1 lg:grid-cols-[1fr_1fr] gap-16 items-center">

        {/* ── LEFT: Copy ── */}
        <div>
          {/* Eyebrow */}
          <div className="animate-on-scroll flex items-center gap-3 mb-8">
            <img
              src="/logo.svg"
              alt="Odontari"
              width={44}
              height={44}
              className="w-11 h-11 object-contain shrink-0"
            />
            <span className="flex items-center gap-1.5 px-3.5 py-1.5 rounded-full
                             bg-teal/15 border border-teal/30 text-teal-light
                             section-eyebrow">
              <span className="w-1.5 h-1.5 rounded-full bg-teal-light animate-pulse" />
              Software Dental · República Dominicana
            </span>
          </div>

          {/* Headline */}
          <h1 className="animate-on-scroll font-display font-extrabold text-white
                         text-[2.6rem] sm:text-[3.4rem] lg:text-[3.8rem] leading-[1.05]
                         tracking-tight mb-6">
            Gestión dental
            <br />
            <span className="text-gradient-sky">sin fricciones</span>
            <span className="text-white">.</span>
          </h1>

          {/* Sub */}
          <p className="animate-on-scroll text-pale/70 text-lg leading-relaxed max-w-md mb-10">
            Agenda, expediente clínico, facturación NCF y consentimiento informado digital.
            Todo en una plataforma moderna, diseñada para clínicas dominicanas.
          </p>

          {/* CTAs */}
          <div className="animate-on-scroll flex flex-col sm:flex-row gap-3 mb-12">
            <a
              href={`${APP_URL}/Identity/Account/Login`}
              className="inline-flex items-center justify-center gap-2 px-7 py-3.5
                         rounded-xl bg-teal hover:bg-teal-light text-white font-semibold
                         text-base transition-all duration-200 shadow-[0_8px_24px_rgba(78,142,162,0.35)]
                         hover:shadow-[0_8px_32px_rgba(78,142,162,0.5)] hover:-translate-y-0.5"
            >
              Probar gratis
              <svg className="w-4 h-4" viewBox="0 0 16 16" fill="none" stroke="currentColor" strokeWidth="2">
                <path d="M3 8h10M9 4l4 4-4 4" strokeLinecap="round" strokeLinejoin="round" />
              </svg>
            </a>
            <a
              href={`https://wa.me/${WHATSAPP}?text=Hola%2C%20quiero%20solicitar%20una%20demo%20de%20ODONTARI`}
              target="_blank"
              rel="noopener noreferrer"
              className="inline-flex items-center justify-center gap-2 px-7 py-3.5
                         rounded-xl border border-white/20 text-white/80 font-medium
                         text-base hover:bg-white/8 hover:text-white hover:border-white/35
                         transition-all duration-200"
            >
              <WhatsAppIcon />
              Solicitar demo
            </a>
          </div>

          {/* Trust row */}
          <div className="animate-on-scroll flex flex-wrap items-center gap-6">
            {[
              { icon: <CloudIcon />, label: "100% en la nube" },
              { icon: <ShieldIcon />, label: "Sin instalación" },
              { icon: <ReceiptSmIcon />, label: "NCF integrado" },
            ].map((t) => (
              <div key={t.label} className="flex items-center gap-2 text-pale/50">
                <span className="text-teal/70">{t.icon}</span>
                <span className="text-xs font-medium">{t.label}</span>
              </div>
            ))}
          </div>
        </div>

        {/* ── RIGHT: Dashboard mockup ── */}
        <div className="animate-on-scroll relative hidden lg:block">
          <DashboardMockup />
        </div>
      </div>

      {/* Bottom fade */}
      <div className="absolute bottom-0 left-0 right-0 h-32
                      bg-gradient-to-t from-navy-deep to-transparent pointer-events-none" />
    </section>
  );
}

/* ─── Dashboard mockup ─── */
function DashboardMockup() {
  const appointments = [
    { time: "09:00", patient: "María García", procedure: "Limpieza dental", status: "confirmed" },
    { time: "10:30", patient: "Carlos Pérez", procedure: "Extracción", status: "active" },
    { time: "11:45", patient: "Ana Rodríguez", procedure: "Ortodoncia", status: "pending" },
    { time: "14:00", patient: "Luis Martínez", procedure: "Blanqueamiento", status: "confirmed" },
  ];

  const statusConfig: Record<string, { label: string; cls: string }> = {
    confirmed: { label: "Confirmada", cls: "bg-teal/20 text-teal-light border border-teal/25" },
    active:    { label: "En curso",   cls: "bg-sky/20 text-sky border border-sky/25" },
    pending:   { label: "Pendiente",  cls: "bg-white/10 text-pale/70 border border-white/15" },
  };

  return (
    <div className="relative">
      {/* Glow behind the card */}
      <div className="absolute inset-8 bg-teal/15 blur-[60px] rounded-full" />

      {/* Main card — Agenda */}
      <div className="relative rounded-2xl overflow-hidden border border-white/12
                      bg-navy-deep/90 backdrop-blur-sm shadow-[0_32px_80px_rgba(0,0,0,0.5)]">
        {/* Window bar */}
        <div className="px-5 py-3.5 border-b border-white/8 flex items-center justify-between">
          <div className="flex items-center gap-2">
            <div className="flex gap-1.5">
              {["#ff5f56","#ffbd2e","#27c93f"].map(c => (
                <div key={c} className="w-3 h-3 rounded-full" style={{ background: c, opacity: 0.7 }} />
              ))}
            </div>
            <span className="ml-2 text-pale/40 text-xs font-mono">Odontari — Agenda</span>
          </div>
          <div className="flex items-center gap-1.5">
            <div className="w-1.5 h-1.5 rounded-full bg-teal animate-pulse" />
            <span className="text-teal text-[10px] font-medium">En vivo</span>
          </div>
        </div>

        {/* Date header */}
        <div className="px-5 py-3 border-b border-white/5 flex items-center justify-between">
          <div>
            <p className="text-white font-semibold text-sm font-display">Agenda de hoy</p>
            <p className="text-pale/40 text-xs">Miércoles, 18 de marzo 2026</p>
          </div>
          <div className="px-3 py-1 rounded-lg bg-teal/15 border border-teal/20">
            <span className="text-teal-light text-xs font-medium">4 citas</span>
          </div>
        </div>

        {/* Appointment list */}
        <div className="divide-y divide-white/5">
          {appointments.map((apt, i) => (
            <div key={i}
              className="px-5 py-3.5 flex items-center gap-4 hover:bg-white/4 transition-colors">
              <span className="text-pale/40 text-xs font-mono w-10 shrink-0">{apt.time}</span>
              <div className="flex-1 min-w-0">
                <p className="text-white text-sm font-medium truncate">{apt.patient}</p>
                <p className="text-pale/50 text-[11px] truncate">{apt.procedure}</p>
              </div>
              <span className={`px-2.5 py-0.5 rounded-full text-[10px] font-semibold shrink-0
                               ${statusConfig[apt.status].cls}`}>
                {statusConfig[apt.status].label}
              </span>
            </div>
          ))}
        </div>

        {/* Footer metrics */}
        <div className="px-5 py-4 border-t border-white/8 grid grid-cols-3 gap-0
                        divide-x divide-white/8">
          {[
            { value: "RD$84K", label: "Este mes" },
            { value: "248", label: "Pacientes" },
            { value: "3", label: "Doctores" },
          ].map((m) => (
            <div key={m.label} className="text-center px-3 first:pl-0 last:pr-0">
              <p className="text-white font-bold text-sm font-display">{m.value}</p>
              <p className="text-pale/40 text-[10px]">{m.label}</p>
            </div>
          ))}
        </div>
      </div>

      {/* Floating mini card — Invoice */}
      <div className="absolute -bottom-6 -left-10 w-52
                      rounded-xl border border-teal/25 bg-navy-mid/95 backdrop-blur-sm
                      p-4 shadow-xl shadow-black/30 animate-float">
        <div className="flex items-center gap-2 mb-2">
          <div className="w-7 h-7 rounded-lg bg-teal/20 flex items-center justify-center">
            <svg className="w-3.5 h-3.5 text-teal-light" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.8">
              <path d="M4 2l2 2 2-2 2 2 2-2 2 2 2-2v18l-2-2-2 2-2-2-2 2-2-2-2 2V2z"/>
              <line x1="8" y1="10" x2="16" y2="10"/>
              <line x1="8" y1="14" x2="13" y2="14"/>
            </svg>
          </div>
          <div>
            <p className="text-white text-xs font-semibold">NCF Generado</p>
            <p className="text-pale/40 text-[10px]">B01-00000042</p>
          </div>
        </div>
        <div className="flex items-center justify-between pt-2 border-t border-white/10">
          <span className="text-pale/50 text-[10px]">ITBIS 18%</span>
          <span className="text-teal-light text-xs font-bold">RD$4,720</span>
        </div>
      </div>

      {/* Floating mini card — Consent */}
      <div className="absolute -top-4 -right-8 w-48
                      rounded-xl border border-sky/20 bg-navy-light/95 backdrop-blur-sm
                      p-4 shadow-xl shadow-black/25 animate-float-slow">
        <div className="flex items-center gap-2">
          <div className="w-7 h-7 rounded-lg bg-sky/15 flex items-center justify-center">
            <svg className="w-3.5 h-3.5 text-sky" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.8">
              <path d="M3 17c1-1 2-3 3-4s2 1 3 0 2-3 3-4 2 0 3 1"/>
              <line x1="3" y1="20" x2="21" y2="20"/>
            </svg>
          </div>
          <div>
            <p className="text-white text-xs font-semibold">Firma digital</p>
            <p className="text-sky/70 text-[10px]">Consentimiento OK</p>
          </div>
        </div>
      </div>
    </div>
  );
}

/* ─── Inline icons ─── */
function WhatsAppIcon() {
  return (
    <svg className="w-4 h-4" viewBox="0 0 24 24" fill="currentColor">
      <path d="M17.472 14.382c-.297-.149-1.758-.867-2.03-.967-.273-.099-.471-.148-.67.15-.197.297-.767.966-.94 1.164-.173.199-.347.223-.644.075-.297-.15-1.255-.463-2.39-1.475-.883-.788-1.48-1.761-1.653-2.059-.173-.297-.018-.458.13-.606.134-.133.298-.347.446-.52.149-.174.198-.298.298-.497.099-.198.05-.371-.025-.52-.075-.149-.669-1.612-.916-2.207-.242-.579-.487-.5-.669-.51-.173-.008-.371-.01-.57-.01-.198 0-.52.074-.792.372-.272.297-1.04 1.016-1.04 2.479 0 1.462 1.065 2.875 1.213 3.074.149.198 2.096 3.2 5.077 4.487.709.306 1.262.489 1.694.625.712.227 1.36.195 1.871.118.571-.085 1.758-.719 2.006-1.413.248-.694.248-1.289.173-1.413-.074-.124-.272-.198-.57-.347zM12 0C5.373 0 0 5.373 0 12c0 2.127.558 4.122 1.532 5.855L.057 23.522a.5.5 0 00.609.61l5.799-1.453A11.944 11.944 0 0012 24c6.627 0 12-5.373 12-12S18.627 0 12 0zm0 21.818a9.81 9.81 0 01-5.032-1.382l-.361-.214-3.741.937.978-3.637-.235-.374A9.818 9.818 0 012.182 12C2.182 6.57 6.57 2.182 12 2.182c5.43 0 9.818 4.388 9.818 9.818 0 5.43-4.388 9.818-9.818 9.818z" />
    </svg>
  );
}
function CloudIcon() {
  return (
    <svg className="w-3.5 h-3.5" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.8">
      <path d="M18 10h-1.26A8 8 0 109 20h9a5 5 0 000-10z" strokeLinecap="round"/>
    </svg>
  );
}
function ShieldIcon() {
  return (
    <svg className="w-3.5 h-3.5" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.8">
      <path d="M12 22s8-4 8-10V5l-8-3-8 3v7c0 6 8 10 8 10z" strokeLinecap="round" strokeLinejoin="round"/>
    </svg>
  );
}
function ReceiptSmIcon() {
  return (
    <svg className="w-3.5 h-3.5" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.8">
      <path d="M4 2l2 2 2-2 2 2 2-2 2 2 2-2v18l-2-2-2 2-2-2-2 2-2-2-2 2V2z" strokeLinecap="round"/>
    </svg>
  );
}
