enum Wrapper {
    Val(i32)
}

fn get_val(w: &Wrapper) -> i32 {
    match w {
        Wrapper.Val(x) => *x,
        _ => 0
    }
}

fn main() -> i32 {
    let w = Wrapper.Val(42);
    get_val(&w)
}
