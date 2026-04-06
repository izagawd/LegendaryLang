enum Holder {
    Val(i32)
}

fn set_inner(h: &mut Holder, v: i32) {
    match h {
        Holder.Val(x) => *x = v,
        _ => {}
    }
}

fn main() -> i32 {
    let h = Holder.Val(0);
    set_inner(&mut h, 77);
    match h {
        Holder.Val(v) => v,
        _ => 0
    }
}
