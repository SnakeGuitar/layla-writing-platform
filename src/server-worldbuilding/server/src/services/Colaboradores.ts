import { Colaborador } from "@/services/ColaboradoresInterfaces";

const users: Colaborador[] = [];

export const getAllUsers = async (): Promise<Colaborador[]> => {
  return users;
};

export const createUser = async (data: {
  name: string;
}): Promise<Colaborador> => {
  const newUser = {
    id: Date.now(),
    name: data.name,
  };

  users.push(newUser);
  return newUser;
};
