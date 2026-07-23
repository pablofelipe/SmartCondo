import { useEffect, useState } from 'react';
import { getUserTypes, UserType } from '../../services/userService';

export const useUserTypes = () => {
  const [userTypes, setUserTypes] = useState<Record<string, UserType>>({});
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    const fetchData = async () => {
      try {
        const data = await getUserTypes();

        const typesMap = data.reduce(
          (acc, type) => {
            acc[type.name] = type;
            return acc;
          },
          {} as Record<string, UserType>,
        );

        setUserTypes(typesMap);
      } catch (error) {
        console.error('Error loading user types:', error);
      } finally {
        setLoading(false);
      }
    };

    fetchData();
  }, []);

  const getNameFromId = (id: number) =>
    Object.values(userTypes).find((t) => t.id === id)?.name;

  const getIdFromName = (name: string) => userTypes[name]?.id;

  return { userTypes, loading, getNameFromId, getIdFromName };
};
