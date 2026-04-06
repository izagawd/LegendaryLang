enum Holder {
    Val(i32)
}

fn main() -> i32 {
    let h = Holder.Val(42);
    match h {
        Holder.Val(v) => v,
        _ => 0
    }
}
