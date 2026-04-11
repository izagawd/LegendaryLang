struct Holder { val: i32 }
impl Copy for Holder {}
impl Holder {
    fn modify(self: &mut Self) -> i32 { self.val }
}

struct Mid['a] {
    h: &'a mut Holder
}

struct Top['a, 'b] {
    mid: &'a mut Mid['b]
}

fn deep(t: &mut Top) -> i32 {
    t.mid.h.modify()
}

fn main() -> i32 {
    let h = make Holder { val: 42 };
    let m = make Mid { h: &mut h };
    let t = make Top { mid: &mut m };
    deep(&mut t)
}
