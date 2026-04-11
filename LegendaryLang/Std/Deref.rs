trait Receiver {
    let Target :! type;
}

trait Deref: Receiver {
    fn deref(self: &Self) -> &Self.Target;
}

trait DerefMut: Deref {
    fn deref_mut(self: &mut Self) -> &mut Self.Target;
}

impl[T:! Sized] Receiver for &T {
    let Target :! type = T;
}
impl[T:! Sized] Deref for &T {
    fn deref(self: &Self) -> &Self.Target {
        self
    }
}

impl[T:! Sized] Receiver for &mut T {
    let Target :! type = T;
}
impl[T:! Sized] Deref for &mut T {
    fn deref(self: &Self) -> &Self.Target {
        self
    }
}
impl[T:! Sized] DerefMut for &mut T {
    fn deref_mut(self: &mut Self) -> &mut Self.Target {
        self
    }
}
