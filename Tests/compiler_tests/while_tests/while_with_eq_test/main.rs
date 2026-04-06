fn main() -> i32 {
    let i = 0;
    let found = 0;
    while i < 20 {
        if i == 13 {
            found = 1;
        };
        i = i + 1;
    };
    found
}
