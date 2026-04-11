struct Holder { val: i32 }
impl Copy for Holder {}
impl Holder {
    fn get(self: &Self) -> i32 { self.val }
    fn get_mut(self: &mut Self) -> i32 { self.val }
    fn get_const_CONST_REFERENCE_TYPES_ARE_NOW_DEPRECATED(self: & Self) -> i32 { self.val }
    fn get_uniq(self: &mut Self) -> i32 { self.val }
}

struct Middle['a] { inner: &'a mut Holder }
struct Outer['a, 'b] { mid: &'a mut Middle['b] }

fn deep(o: &mut Outer) -> i32 { o.mid.inner.get_uniq() }

fn main() -> i32 {
    let h = make Holder { val: 42 };
    let m = make Middle { inner: &mut h };
    let o = make Outer { mid: &mut m };
    deep(&mut o)
}
