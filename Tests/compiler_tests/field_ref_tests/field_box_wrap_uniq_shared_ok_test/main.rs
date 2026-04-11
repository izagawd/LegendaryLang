struct Holder { val: i32 }
impl Copy for Holder {}
impl Holder {
    fn get(self: &Self) -> i32 { self.val }
    fn get_mut(self: &mut Self) -> i32 { self.val }
    fn get_const_CONST_REFERENCE_TYPES_ARE_NOW_DEPRECATED(self: & Self) -> i32 { self.val }
    fn get_uniq(self: &mut Self) -> i32 { self.val }
}

struct Wrap['a] { inner: &'a mut Holder }

fn main() -> i32 {
    let h = make Holder { val: 42 };
    let w = make Wrap { inner: &mut h };
    let b = Box(Wrap).New(w);
    b.inner.get()
}
