struct Holder { val: i32 }
impl Copy for Holder {}
impl Holder {
    fn get(self: &Self) -> i32 { self.val }
    fn get_mut(self: &mut Self) -> i32 { self.val }
}

struct Wrap['a] { inner: &'a mut Holder }

fn through(w: &Wrap) -> i32 { w.inner.get() }

fn main() -> i32 {
    let h = make Holder { val: 42 };
    let w = make Wrap { inner: &mut h };
    through(&w)
}
