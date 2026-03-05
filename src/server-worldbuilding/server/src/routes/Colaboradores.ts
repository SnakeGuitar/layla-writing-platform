import { Router, Response } from "express";
import { generatejwtTokens, verifyRefreshToken } from "@/utils/ManageJWT";
import {
  MiddlewareAuthenticate,
  MiddlewareRequireRole,
} from "@/middlewares/Auth";
import LoginInterface from "@/interfaces/auth/Login";
import JwtPayloadCustom from "@/interfaces/auth/JwtPayloadCustom";
import AuthRequest from "@/interfaces/auth/AuthRequest";

const router: ReturnType<typeof Router> = Router();

// Obtener perfil del usuario actual
router.get("/me", MiddlewareAuthenticate, (req: AuthRequest, res: Response) => {
  res.json({
    user: req.user,
  });
});

// Ruta solo para administradores
router.get(
  "/admin",
  MiddlewareAuthenticate,
  MiddlewareRequireRole("admin"),
  (req: AuthRequest, res: Response) => {
    res.json({ message: "Bienvenido, administrador" });
  },
);

// Login
router.post("/login", async (req, res) => {
  const login: LoginInterface = req.body;
  // TODO-Desarrollo: Database logic

  const currentUser: JwtPayloadCustom = {
    id: 1,
    email: "example@example.com",
    role: "ExampleRole",
  };

  // Generar tokens
  const tokens = generatejwtTokens(currentUser);

  // Devolver tokens (en producción, considera guardar refreshToken en httpOnly cookie)
  res.json(tokens);
});

// Refresh token
router.post("/refresh", (req, res) => {
  const refreshToken: string = req.body;

  if (!refreshToken) {
    res.status(400).json({ error: "Refresh token required" });
    return;
  }

  try {
    const decoded = verifyRefreshToken(refreshToken);

    // Aquí buscarías el usuario en tu DB usando decoded.id
    const user: JwtPayloadCustom = {
      id: 1,
      email: "example@example.com",
      role: "ExampleRole",
    };

    // Generar nuevo par de tokens
    const tokens = generatejwtTokens(user);

    res.json(tokens);
  } catch {
    res.status(401).json({ error: "Invalid refresh token" });
  }
});

// Logout (invalidar refresh token en DB si lo almacenas)
router.post("/logout", MiddlewareAuthenticate, (req, res) => {
  // Aquí eliminarías el refreshToken de tu DB
  res.json({ message: "Logged out successfully" });
});

export default router;
