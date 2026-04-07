trait Receiver {
    let Target :! type;
}

trait Deref: Receiver {
    fn deref(self: &Self) -> &Self.Target;
}

trait DerefConst: Deref {
    fn deref_const(self: &const Self) -> &const Self.Target;
}

trait DerefMut: Deref {
    fn deref_mut(self: &mut Self) -> &mut Self.Target;
}

trait DerefUniq: Deref + DerefConst + DerefMut {
    fn deref_uniq(self: &uniq Self) -> &uniq Self.Target;
}

impl[T:! type] Receiver for &T {
    let Target :! type = T;
}
impl[T:! type] Deref for &T {
    fn deref(self: &Self) -> &Self.Target {
        self
    }
}

impl[T:! type] Receiver for &const T {
    let Target :! type = T;
}
impl[T:! type] Deref for &const T {
    fn deref(self: &Self) -> &Self.Target {
        self
    }
}
impl[T:! type] DerefConst for &const T {
    fn deref_const(self: &const Self) -> &const Self.Target {
        self
    }
}

impl[T:! type] Receiver for &mut T {
    let Target :! type = T;
}
impl[T:! type] Deref for &mut T {
    fn deref(self: &Self) -> &Self.Target {
        self
    }
}
impl[T:! type] DerefMut for &mut T {
    fn deref_mut(self: &mut Self) -> &mut Self.Target {
        self
    }
}

impl[T:! type] Receiver for &uniq T {
    let Target :! type = T;
}
impl[T:! type] Deref for &uniq T {
    fn deref(self: &Self) -> &Self.Target {
        self
    }
}
impl[T:! type] DerefConst for &uniq T {
    fn deref_const(self: &const Self) -> &const Self.Target {
        self
    }
}
impl[T:! type] DerefMut for &uniq T {
    fn deref_mut(self: &mut Self) -> &mut Self.Target {
        self
    }
}
impl[T:! type] DerefUniq for &uniq T {
    fn deref_uniq(self: &uniq Self) -> &uniq Self.Target {
        self
    }
}
