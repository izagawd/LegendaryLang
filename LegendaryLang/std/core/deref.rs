trait Deref {
    type Target;
    fn deref(self_ref: &Self) -> &Target;
}

trait DerefConst: Deref {
    fn deref_const(self_ref: &const Self) -> &const Target;
}

trait DerefMut: Deref {
    fn deref_mut(self_ref: &mut Self) -> &mut Target;
}

trait DerefUniq: Deref + DerefConst + DerefMut {
    fn deref_uniq(self_ref: &uniq Self) -> &uniq Target;
}
