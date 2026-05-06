const APP_URL = process.env.NEXT_PUBLIC_APP_URL || "http://localhost:5000";
const WHATSAPP = process.env.NEXT_PUBLIC_WHATSAPP || "18091234567";
const CONTACT_EMAIL = process.env.NEXT_PUBLIC_CONTACT_EMAIL || "soporte@odontari.com";

const navLinks = [
  { label: "Características", href: "#features" },
  { label: "Cómo funciona", href: "#how-it-works" },
  { label: "Planes", href: "#planes" },
  { label: "FAQ", href: "#faq" },
  { label: "Contacto", href: "#contacto" },
];

const legalLinks = [
  { label: "Términos y condiciones", href: "/terminos" },
  { label: "Política de privacidad", href: "/privacidad" },
];

export default function Footer() {
  return (
    <footer className="bg-[#000f20] border-t border-white/6">
      {/* Main content */}
      <div className="max-w-6xl mx-auto px-6 py-16
                      grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-10">

        {/* Brand */}
        <div className="lg:col-span-1">
          <div className="flex items-center gap-2 mb-5">
            <img
              src="/logo.svg"
              alt="Odontari logo"
              width={34}
              height={34}
              className="w-9 h-9 object-contain shrink-0"
            />
            <span className="font-display font-bold text-white text-[1.1rem] tracking-tight">
              Odontari
            </span>
          </div>
          <p className="text-pale/35 text-xs leading-relaxed mb-6 max-w-[200px]">
            El sistema inteligente para clínicas odontológicas modernas en República Dominicana.
          </p>
          <div className="flex gap-2.5">
            <SocialLink href="#" label="Instagram">
              <svg className="w-3.5 h-3.5" viewBox="0 0 24 24" fill="currentColor">
                <path d="M12 2.163c3.204 0 3.584.012 4.85.07 3.252.148 4.771 1.691 4.919 4.919.058 1.265.069 1.645.069 4.849 0 3.205-.012 3.584-.069 4.849-.149 3.225-1.664 4.771-4.919 4.919-1.266.058-1.644.07-4.85.07-3.204 0-3.584-.012-4.849-.07-3.26-.149-4.771-1.699-4.919-4.92-.058-1.265-.07-1.644-.07-4.849 0-3.204.013-3.583.07-4.849.149-3.227 1.664-4.771 4.919-4.919 1.266-.057 1.645-.069 4.849-.069zm0-2.163c-3.259 0-3.667.014-4.947.072-4.358.2-6.78 2.618-6.98 6.98-.059 1.281-.073 1.689-.073 4.948 0 3.259.014 3.668.072 4.948.2 4.358 2.618 6.78 6.98 6.98 1.281.058 1.689.072 4.948.072 3.259 0 3.668-.014 4.948-.072 4.354-.2 6.782-2.618 6.979-6.98.059-1.28.073-1.689.073-4.948 0-3.259-.014-3.667-.072-4.947-.196-4.354-2.617-6.78-6.979-6.98-1.281-.059-1.69-.073-4.949-.073zm0 5.838c-3.403 0-6.162 2.759-6.162 6.162s2.759 6.163 6.162 6.163 6.162-2.759 6.162-6.163c0-3.403-2.759-6.162-6.162-6.162zm0 10.162c-2.209 0-4-1.79-4-4 0-2.209 1.791-4 4-4s4 1.791 4 4c0 2.21-1.791 4-4 4zm6.406-11.845c-.796 0-1.441.645-1.441 1.44s.645 1.44 1.441 1.44c.795 0 1.439-.645 1.439-1.44s-.644-1.44-1.439-1.44z" />
              </svg>
            </SocialLink>
            <SocialLink href={`https://wa.me/${WHATSAPP}`} label="WhatsApp">
              <svg className="w-3.5 h-3.5" viewBox="0 0 24 24" fill="currentColor">
                <path d="M17.472 14.382c-.297-.149-1.758-.867-2.03-.967-.273-.099-.471-.148-.67.15-.197.297-.767.966-.94 1.164-.173.199-.347.223-.644.075-.297-.15-1.255-.463-2.39-1.475-.883-.788-1.48-1.761-1.653-2.059-.173-.297-.018-.458.13-.606.134-.133.298-.347.446-.52.149-.174.198-.298.298-.497.099-.198.05-.371-.025-.52-.075-.149-.669-1.612-.916-2.207-.242-.579-.487-.5-.669-.51-.173-.008-.371-.01-.57-.01-.198 0-.52.074-.792.372-.272.297-1.04 1.016-1.04 2.479 0 1.462 1.065 2.875 1.213 3.074.149.198 2.096 3.2 5.077 4.487.709.306 1.262.489 1.694.625.712.227 1.36.195 1.871.118.571-.085 1.758-.719 2.006-1.413.248-.694.248-1.289.173-1.413-.074-.124-.272-.198-.57-.347zM12 0C5.373 0 0 5.373 0 12c0 2.127.558 4.122 1.532 5.855L.057 23.522a.5.5 0 00.609.61l5.799-1.453A11.944 11.944 0 0012 24c6.627 0 12-5.373 12-12S18.627 0 12 0zm0 21.818a9.81 9.81 0 01-5.032-1.382l-.361-.214-3.741.937.978-3.637-.235-.374A9.818 9.818 0 012.182 12C2.182 6.57 6.57 2.182 12 2.182c5.43 0 9.818 4.388 9.818 9.818 0 5.43-4.388 9.818-9.818 9.818z" />
              </svg>
            </SocialLink>
          </div>
        </div>

        {/* Navigation */}
        <div>
          <p className="text-white/50 text-[10px] font-bold uppercase tracking-widest mb-4">
            Navegación
          </p>
          <ul className="space-y-2.5">
            {navLinks.map((l) => (
              <li key={l.href}>
                <a href={l.href}
                   className="text-pale/40 hover:text-pale/70 text-xs transition-colors">
                  {l.label}
                </a>
              </li>
            ))}
          </ul>
        </div>

        {/* Contact */}
        <div>
          <p className="text-white/50 text-[10px] font-bold uppercase tracking-widest mb-4">
            Contacto
          </p>
          <ul className="space-y-3">
            <li>
              <a href={`mailto:${CONTACT_EMAIL}`}
                 className="flex items-center gap-2 text-pale/40 hover:text-pale/70
                            text-xs transition-colors">
                <svg className="w-3.5 h-3.5 shrink-0" viewBox="0 0 24 24" fill="none"
                     stroke="currentColor" strokeWidth="1.8">
                  <rect x="2" y="4" width="20" height="16" rx="2"/>
                  <path d="M2 7l10 7 10-7" strokeLinecap="round"/>
                </svg>
                {CONTACT_EMAIL}
              </a>
            </li>
            <li>
              <a href={`https://wa.me/${WHATSAPP}`} target="_blank" rel="noopener noreferrer"
                 className="flex items-center gap-2 text-pale/40 hover:text-pale/70
                            text-xs transition-colors">
                <svg className="w-3.5 h-3.5 shrink-0" viewBox="0 0 24 24" fill="currentColor">
                  <path d="M17.472 14.382c-.297-.149-1.758-.867-2.03-.967-.273-.099-.471-.148-.67.15-.197.297-.767.966-.94 1.164-.173.199-.347.223-.644.075-.297-.15-1.255-.463-2.39-1.475-.883-.788-1.48-1.761-1.653-2.059-.173-.297-.018-.458.13-.606.134-.133.298-.347.446-.52.149-.174.198-.298.298-.497.099-.198.05-.371-.025-.52-.075-.149-.669-1.612-.916-2.207-.242-.579-.487-.5-.669-.51-.173-.008-.371-.01-.57-.01-.198 0-.52.074-.792.372-.272.297-1.04 1.016-1.04 2.479 0 1.462 1.065 2.875 1.213 3.074.149.198 2.096 3.2 5.077 4.487.709.306 1.262.489 1.694.625.712.227 1.36.195 1.871.118.571-.085 1.758-.719 2.006-1.413.248-.694.248-1.289.173-1.413-.074-.124-.272-.198-.57-.347zM12 0C5.373 0 0 5.373 0 12c0 2.127.558 4.122 1.532 5.855L.057 23.522a.5.5 0 00.609.61l5.799-1.453A11.944 11.944 0 0012 24c6.627 0 12-5.373 12-12S18.627 0 12 0zm0 21.818a9.81 9.81 0 01-5.032-1.382l-.361-.214-3.741.937.978-3.637-.235-.374A9.818 9.818 0 012.182 12C2.182 6.57 6.57 2.182 12 2.182c5.43 0 9.818 4.388 9.818 9.818 0 5.43-4.388 9.818-9.818 9.818z" />
                </svg>
                WhatsApp
              </a>
            </li>
            <li className="flex items-center gap-2 text-pale/35 text-xs">
              <svg className="w-3.5 h-3.5 shrink-0" viewBox="0 0 24 24" fill="none"
                   stroke="currentColor" strokeWidth="1.8">
                <circle cx="12" cy="12" r="10"/>
                <path d="M12 6v6l4 2" strokeLinecap="round"/>
              </svg>
              Lun–Vie 8:00 AM – 6:00 PM
            </li>
          </ul>
        </div>

        {/* Legal + CTA */}
        <div>
          <p className="text-white/50 text-[10px] font-bold uppercase tracking-widest mb-4">
            Legal
          </p>
          <ul className="space-y-2.5 mb-6">
            {legalLinks.map((l) => (
              <li key={l.href}>
                <a href={l.href}
                   className="text-pale/40 hover:text-pale/70 text-xs transition-colors">
                  {l.label}
                </a>
              </li>
            ))}
          </ul>
          <a
            href={`${APP_URL}/Identity/Account/Login`}
            className="inline-flex items-center gap-1.5 px-4 py-2.5 rounded-lg
                       bg-teal/15 border border-teal/25 text-teal-light text-xs font-semibold
                       hover:bg-teal/25 transition-colors"
          >
            Iniciar sesión
            <svg className="w-3 h-3" viewBox="0 0 12 12" fill="none"
                 stroke="currentColor" strokeWidth="2" strokeLinecap="round">
              <path d="M2 6h8M6 3l3 3-3 3" />
            </svg>
          </a>
        </div>
      </div>

      {/* Bottom bar */}
      <div className="border-t border-white/5">
        <div className="max-w-6xl mx-auto px-6 py-5 flex flex-col sm:flex-row
                        items-center justify-between gap-2">
          <p className="text-pale/25 text-xs">
            &copy; 2026 Odontari. Todos los derechos reservados.
          </p>
          <p className="text-pale/25 text-xs">
            Hecho en República Dominicana 🇩🇴
          </p>
        </div>
      </div>
    </footer>
  );
}

function SocialLink({ href, label, children }: {
  href: string;
  label: string;
  children: React.ReactNode;
}) {
  return (
    <a
      href={href}
      target="_blank"
      rel="noopener noreferrer"
      aria-label={label}
      className="w-8 h-8 rounded-lg bg-white/6 border border-white/8
                 hover:bg-white/12 hover:border-teal/25 flex items-center justify-center
                 text-pale/40 hover:text-pale/80 transition-all duration-200"
    >
      {children}
    </a>
  );
}
