trait Receiver {
    type Target;
}

trait Deref: Receiver {
    fn deref(self: &Self) -> &Target;
}

trait DerefConst: Deref {
    fn deref_const(self: &const Self) -> &const Target;
}

trait DerefMut: Deref {
    fn deref_mut(self: &mut Self) -> &mut Target;
}

trait DerefUniq: Deref + DerefConst + DerefMut {
    fn deref_uniq(self: &uniq Self) -> &uniq Target;
}
