fn main() -> i32 {
    let i = 0;
    let count = 0;
    while i < 100 {
        if i > 10 && i < 20 {
            count = count + 1;
        };
        i = i + 1;
    };
    count
}
