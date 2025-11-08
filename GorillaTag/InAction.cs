namespace GorillaTag;

public delegate void InAction<T>(in T obj);
public delegate void InAction<T1, T2>(in T1 obj1, in T2 obj2);
public delegate void InAction<T1, T2, T3>(in T1 obj1, in T2 obj2, in T3 obj3);
public delegate void InAction<T1, T2, T3, T4>(in T1 obj1, in T2 obj2, in T3 obj3, in T4 obj4);
