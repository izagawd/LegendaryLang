struct Holder {
    a: &uniq i32,
    b: &uniq i32
}

fn main() -> i32 {
    let x = 0;
    let h = make Holder {
        a: &uniq x,
        b: &uniq x
    };
    0
}
