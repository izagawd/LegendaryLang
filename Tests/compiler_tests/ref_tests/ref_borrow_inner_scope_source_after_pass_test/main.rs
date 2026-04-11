struct Holder['a] {
    r: &'a mut i32
}

fn main() -> i32 {
    let a = 5;
    {
        let h = make Holder { r: &mut a };
    };
    a
}
