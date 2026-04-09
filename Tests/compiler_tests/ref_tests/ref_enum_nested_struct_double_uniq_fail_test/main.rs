struct Holder {
    r: &uniq i32
}

enum Wrapper {
    Two(Holder, Holder)
}

fn main() -> i32 {
    let x = 0;
    let w = Wrapper.Two(
        make Holder { r: &uniq x },
        make Holder { r: &uniq x }
    );
    0
}
