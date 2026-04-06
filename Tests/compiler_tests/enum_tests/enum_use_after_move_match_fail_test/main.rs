enum Wrapper {
    Val(i32)
}

fn consume(w: Wrapper) -> i32 {
    match w {
        Wrapper.Val(v) => v,
        _ => 0
    }
}

fn main() -> i32 {
    let w = Wrapper.Val(5);
    consume(w);
    consume(w)
}
