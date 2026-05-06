import type { Metadata } from "next";
import "./globals.css";

export const metadata: Metadata = {
  title: "Odontari · Software Dental para República Dominicana",
  description:
    "Software dental multi-usuario con agenda inteligente, expediente clínico digital, facturación NCF y consentimiento informado digital. Diseñado para clínicas en República Dominicana.",
  openGraph: {
    title: "Odontari — Gestión Odontológica",
    description:
      "Agenda, expediente clínico, facturación NCF y más. El sistema dental moderno para RD.",
    type: "website",
  },
};

export default function RootLayout({
  children,
}: Readonly<{
  children: React.ReactNode;
}>) {
  return (
    <html lang="es">
      <body>{children}</body>
    </html>
  );
}
