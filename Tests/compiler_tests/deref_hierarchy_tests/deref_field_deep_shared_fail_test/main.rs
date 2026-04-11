struct Holder { val: i32 }
impl Copy for Holder {}
impl Holder {
    fn modify(self: &mut Self) -> i32 { self.val }
}

struct Inner['a] {
    h: &'a mut Holder
}

struct Outer['a, 'b] {
    inner: &'a mut Inner['b]
}

fn deep_modify(o: &Outer) -> i32 {
    o.inner.h.modify()
}

fn main() -> i32 {
    let h = make Holder { val: 42 };
    let i = make Inner { h: &mut h };
    let o = make Outer { inner: &mut i };
    deep_modify(&o)
}
