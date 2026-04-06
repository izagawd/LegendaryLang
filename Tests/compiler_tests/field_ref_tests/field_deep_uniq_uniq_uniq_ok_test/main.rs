struct Holder { val: i32 }
impl Copy for Holder {}
impl Holder {
    fn get(self: &Self) -> i32 { self.val }
    fn get_mut(self: &mut Self) -> i32 { self.val }
    fn get_const(self: &const Self) -> i32 { self.val }
    fn get_uniq(self: &uniq Self) -> i32 { self.val }
}

struct Middle['a] { inner: &'a uniq Holder }
struct Outer['a, 'b] { mid: &'a uniq Middle('b) }

fn deep(o: &uniq Outer) -> i32 { o.mid.inner.get_uniq() }

fn main() -> i32 {
    let h = make Holder { val: 42 };
    let m = make Middle { inner: &uniq h };
    let o = make Outer { mid: &uniq m };
    deep(&uniq o)
}
