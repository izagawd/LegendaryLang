struct Holder {
    a: &mut i32,
    b: &mut i32
}

fn main() -> i32 {
    let x = 0;
    let h = make Holder {
        a: &mut x,
        b: &mut x
    };
    0
}
