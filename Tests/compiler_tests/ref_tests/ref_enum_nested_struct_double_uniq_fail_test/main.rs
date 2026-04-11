struct Holder {
    r: &mut i32
}

enum Wrapper {
    Two(Holder, Holder)
}

fn main() -> i32 {
    let x = 0;
    let w = Wrapper.Two(
        make Holder { r: &mut x },
        make Holder { r: &mut x }
    );
    0
}
