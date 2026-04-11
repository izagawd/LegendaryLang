enum Holder {
    Val(i32),
    Empty
}

fn set(h: &mut Holder, v: i32) {
    match h {
        Holder.Val(x) => *x = v,
        Holder.Empty => {}
    }
}

fn main() -> i32 {
    let h = Holder.Val(0);
    set(&mut h, 42);
    set(&mut h, 77);
    match h {
        Holder.Val(v) => v,
        Holder.Empty => 0
    }
}
