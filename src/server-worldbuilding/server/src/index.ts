import dotenv from "dotenv";
import express from "express";

dotenv.config();
const app = express();

app.use(express.json());
app.listen(3000, () => {
  console.log("Servidor corriendo en http://localhost:3516");
});
