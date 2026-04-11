struct Holder {
    val: i32
}
impl Copy for Holder {}
impl Holder {
    fn modify(self: &mut Self) -> i32 { self.val }
}

struct Middle['a] {
    target: &'a mut Holder
}

struct Outer['a, 'b] {
    mid: &'a mut Middle['b]
}

fn deep_modify(o: &Outer) -> i32 {
    o.mid.target.modify()
}

fn main() -> i32 {
    let h = make Holder { val: 42 };
    let m = make Middle { target: &mut h };
    let o = make Outer { mid: &mut m };
    deep_modify(&o)
}
