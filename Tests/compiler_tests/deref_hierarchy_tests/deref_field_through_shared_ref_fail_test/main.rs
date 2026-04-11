struct Holder {
    val: i32
}
impl Copy for Holder {}
impl Holder {
    fn modify(self: &mut Self) -> i32 { self.val }
}

struct Wrapper['a] {
    inner: &'a mut Holder
}

fn modify_through(w: &Wrapper) -> i32 {
    w.inner.modify()
}

fn main() -> i32 {
    let h = make Holder { val: 42 };
    let w = make Wrapper { inner: &mut h };
    modify_through(&w)
}
