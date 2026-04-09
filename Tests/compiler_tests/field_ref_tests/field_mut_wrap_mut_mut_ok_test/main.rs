struct Holder { val: i32 }
impl Copy for Holder {}
impl Holder {
    fn get(self: &Self) -> i32 { self.val }
    fn get_mut(self: &mut Self) -> i32 { self.val }
    fn get_const(self: &const Self) -> i32 { self.val }
    fn get_uniq(self: &uniq Self) -> i32 { self.val }
}

struct WrapMut['a] { inner: &'a mut Holder }

fn through(w: &mut WrapMut) -> i32 { w.inner.get_mut() }

fn main() -> i32 {
    let h = make Holder { val: 42 };
    let w = make WrapMut { inner: &mut h };
    through(&mut w)
}
